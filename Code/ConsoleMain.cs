using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Security.Policy;
using System.Security.Permissions;
using System.Security;
using System.IO;
using Microsoft.Scripting.Hosting;
using IronPython.Hosting;
using System.Runtime.Remoting;
using System.Net;
using Microsoft.Scripting;

namespace CodeProject.CodeConsole {
	public class ConsoleMain {
		private const string IRON_PYTHON_EXCEPTION_NS = "IronPython";

		private AppDomain sandboxDomain;
		private ScriptEngine engine;
		private ScriptScope scope;

		public ConsoleMain() {
			try {
				CreateAppDomain();
				CreateEngine();
				RunLoop();
			}
			finally {
				if(sandboxDomain != null) {
					AppDomain.Unload(sandboxDomain);
				}
			}
		}

		private void CreateEngine() {
			engine = Python.CreateEngine(sandboxDomain);
			scope = engine.CreateScope();
		}

		private void CreateAppDomain() {
			var setup = new AppDomainSetup();
			setup.ApplicationBase = AppDomain.CurrentDomain.BaseDirectory;

			PermissionSet permSet = new PermissionSet(PermissionState.None);
			permSet.AddPermission(new SecurityPermission(SecurityPermissionFlag.Execution));

			StrongName[] TrustedAssemblies = new StrongName[] {
                //GetStrongName(typeof(Enumerable))
            };

			sandboxDomain = AppDomain.CreateDomain("Sandbox", null, setup, permSet);
		}

		private StrongName GetStrongName(Type t) {
			return t.Assembly.Evidence.GetHostEvidence<StrongName>();
		}

		private void RunLoop() {
			RunCode("import System\nfrom System.Collections import *\nh = Hashtable()\n");

			while(true) {
				string code = Console.ReadLine();

				RunCode(code);

				/*
				 * Python code that works:
				 * 
				 * import System
				 * from System.Collections import *
				 * h = Hashtable()
				 * h["a"] = "IronPython"
				 * h["b"] = "Tutorial"
				 * for e in h: print e.Key, ":", e.Value
				 */
			}
		}

		private void RunCode(string code) {
			try {
				try {
					ScriptSource source = engine.CreateScriptSourceFromString(code, SourceCodeKind.AutoDetect);
					CompiledCode compiled = source.Compile();

					// Executes in the scope of Python
					object result = compiled.Execute(scope);
					if(result != null) {
						Console.WriteLine(result.ToString());
					}
				}
				catch(Exception e) {
					handlePyException(e);
				}
			}
			catch(Exception e) {
				Console.WriteLine("Failed to process console command" + e);
			}
		}

		private static void handlePyException(Exception e) {
			if(e.GetType().Namespace.StartsWith(IRON_PYTHON_EXCEPTION_NS)) {
				Console.WriteLine("Python exception: " + e);
			}
			else {
				Console.WriteLine("Internal Engine Error!" + e);
			}
		}

		public static void Main() {
			new ConsoleMain();
		}
	}
}
