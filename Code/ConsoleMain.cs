using System;
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

			var ps = new PermissionSet(PermissionState.None);
			ps.AddPermission(new SecurityPermission(SecurityPermissionFlag.Execution));
			ps.AddPermission(new FileIOPermission(FileIOPermissionAccess.Read | FileIOPermissionAccess.PathDiscovery, AppDomain.CurrentDomain.BaseDirectory));

			var TrustedAssemblies = new Type[] 
            {
                typeof(DateTime),
                typeof(Uri),
                typeof(IronPython.BytesConversionAttribute),
                typeof(IronPython.Modules.ArrayModule),
                typeof(Enumerable)
            }.Select(t => t.Assembly.Evidence.GetHostEvidence<StrongName>()).ToArray();

			sandboxDomain = AppDomain.CreateDomain("Sandbox", null, setup, ps, TrustedAssemblies);
		}

		private void RunLoop() {
			while(true) {
				string code = Console.ReadLine();

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
