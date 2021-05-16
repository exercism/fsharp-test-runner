module Exercism.TestRunner.FSharp.Program

open System
open System.Collections.Concurrent
open System.Diagnostics
open System.IO
open System.Reflection
open System.Threading

open FSharp.Compiler.SourceCodeServices
open Xunit.Runners

let checker = FSharpChecker.Create()

let projectArgs =
    let references =
        AppContext
            .GetData("TRUSTED_PLATFORM_ASSEMBLIES")
            .ToString()
            .Split(Path.PathSeparator)

    [| "/usr/local/share/dotnet/sdk/5.0.103/FSharp/fsc.exe"
       "-o:/Users/erik/Code/exercism/fsharp-test-runner/src/Exercism.TestRunner.FSharp/bin/Debug/net5.0/Fake.dll"
       "-g"
       "--debug:portable"
       "--noframework"
       "--define:TRACE"
       "--define:DEBUG"
       "--define:NET"
       "--define:NET5_0"
       "--define:NETCOREAPP"
       "--optimize-"
       "--tailcalls-"
       "-r:/Users/erik/.nuget/packages/fsharp.core/5.0.0/lib/netstandard2.0/FSharp.Core.dll"
       "-r:/Users/erik/.nuget/packages/fsunit.xunit/4.0.4/lib/netstandard2.0/FsUnit.Xunit.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/Microsoft.CSharp.dll"
       "-r:/Users/erik/.nuget/packages/microsoft.testplatform.testhost/16.9.4/lib/netcoreapp2.1/Microsoft.TestPlatform.CommunicationUtilities.dll"
       "-r:/Users/erik/.nuget/packages/microsoft.testplatform.testhost/16.9.4/lib/netcoreapp2.1/Microsoft.TestPlatform.CoreUtilities.dll"
       "-r:/Users/erik/.nuget/packages/microsoft.testplatform.testhost/16.9.4/lib/netcoreapp2.1/Microsoft.TestPlatform.CrossPlatEngine.dll"
       "-r:/Users/erik/.nuget/packages/microsoft.testplatform.testhost/16.9.4/lib/netcoreapp2.1/Microsoft.TestPlatform.PlatformAbstractions.dll"
       "-r:/Users/erik/.nuget/packages/microsoft.testplatform.testhost/16.9.4/lib/netcoreapp2.1/Microsoft.TestPlatform.Utilities.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/Microsoft.VisualBasic.Core.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/Microsoft.VisualBasic.dll"
       "-r:/Users/erik/.nuget/packages/microsoft.codecoverage/16.9.4/lib/netcoreapp1.0/Microsoft.VisualStudio.CodeCoverage.Shim.dll"
       "-r:/Users/erik/.nuget/packages/microsoft.testplatform.testhost/16.9.4/lib/netcoreapp2.1/Microsoft.VisualStudio.TestPlatform.Common.dll"
       "-r:/Users/erik/.nuget/packages/microsoft.testplatform.testhost/16.9.4/lib/netcoreapp2.1/Microsoft.VisualStudio.TestPlatform.ObjectModel.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/Microsoft.Win32.Primitives.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/mscorlib.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/netstandard.dll"
       "-r:/Users/erik/.nuget/packages/newtonsoft.json/9.0.1/lib/netstandard1.0/Newtonsoft.Json.dll"
       "-r:/Users/erik/.nuget/packages/nhamcrest/2.0.1/lib/netstandard1.5/NHamcrest.dll"
       "-r:/Users/erik/.nuget/packages/nuget.frameworks/5.0.0/lib/netstandard2.0/NuGet.Frameworks.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.AppContext.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Buffers.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Collections.Concurrent.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Collections.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Collections.Immutable.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Collections.NonGeneric.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Collections.Specialized.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.ComponentModel.Annotations.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.ComponentModel.DataAnnotations.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.ComponentModel.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.ComponentModel.EventBasedAsync.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.ComponentModel.Primitives.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.ComponentModel.TypeConverter.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Configuration.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Console.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Core.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Data.Common.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Data.DataSetExtensions.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Data.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Diagnostics.Contracts.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Diagnostics.Debug.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Diagnostics.DiagnosticSource.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Diagnostics.FileVersionInfo.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Diagnostics.Process.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Diagnostics.StackTrace.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Diagnostics.TextWriterTraceListener.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Diagnostics.Tools.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Diagnostics.TraceSource.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Diagnostics.Tracing.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Drawing.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Drawing.Primitives.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Dynamic.Runtime.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Formats.Asn1.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Globalization.Calendars.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Globalization.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Globalization.Extensions.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.IO.Compression.Brotli.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.IO.Compression.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.IO.Compression.FileSystem.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.IO.Compression.ZipFile.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.IO.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.IO.FileSystem.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.IO.FileSystem.DriveInfo.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.IO.FileSystem.Primitives.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.IO.FileSystem.Watcher.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.IO.IsolatedStorage.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.IO.MemoryMappedFiles.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.IO.Pipes.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.IO.UnmanagedMemoryStream.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Linq.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Linq.Expressions.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Linq.Parallel.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Linq.Queryable.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Memory.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Net.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Net.Http.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Net.Http.Json.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Net.HttpListener.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Net.Mail.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Net.NameResolution.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Net.NetworkInformation.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Net.Ping.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Net.Primitives.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Net.Requests.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Net.Security.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Net.ServicePoint.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Net.Sockets.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Net.WebClient.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Net.WebHeaderCollection.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Net.WebProxy.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Net.WebSockets.Client.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Net.WebSockets.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Numerics.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Numerics.Vectors.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.ObjectModel.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Reflection.DispatchProxy.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Reflection.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Reflection.Emit.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Reflection.Emit.ILGeneration.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Reflection.Emit.Lightweight.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Reflection.Extensions.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Reflection.Metadata.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Reflection.Primitives.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Reflection.TypeExtensions.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Resources.Reader.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Resources.ResourceManager.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Resources.Writer.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Runtime.CompilerServices.Unsafe.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Runtime.CompilerServices.VisualC.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Runtime.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Runtime.Extensions.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Runtime.Handles.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Runtime.InteropServices.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Runtime.InteropServices.RuntimeInformation.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Runtime.Intrinsics.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Runtime.Loader.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Runtime.Numerics.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Runtime.Serialization.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Runtime.Serialization.Formatters.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Runtime.Serialization.Json.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Runtime.Serialization.Primitives.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Runtime.Serialization.Xml.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Security.Claims.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Security.Cryptography.Algorithms.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Security.Cryptography.Csp.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Security.Cryptography.Encoding.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Security.Cryptography.Primitives.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Security.Cryptography.X509Certificates.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Security.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Security.Principal.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Security.SecureString.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.ServiceModel.Web.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.ServiceProcess.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Text.Encoding.CodePages.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Text.Encoding.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Text.Encoding.Extensions.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Text.Encodings.Web.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Text.Json.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Text.RegularExpressions.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Threading.Channels.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Threading.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Threading.Overlapped.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Threading.Tasks.Dataflow.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Threading.Tasks.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Threading.Tasks.Extensions.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Threading.Tasks.Parallel.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Threading.Thread.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Threading.ThreadPool.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Threading.Timer.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Transactions.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Transactions.Local.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.ValueTuple.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Web.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Web.HttpUtility.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Windows.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Xml.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Xml.Linq.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Xml.ReaderWriter.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Xml.Serialization.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Xml.XDocument.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Xml.XmlDocument.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Xml.XmlSerializer.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Xml.XPath.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/System.Xml.XPath.XDocument.dll"
       "-r:/Users/erik/.nuget/packages/microsoft.testplatform.testhost/16.9.4/lib/netcoreapp2.1/testhost.dll"
       "-r:/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/WindowsBase.dll"
       "-r:/Users/erik/.nuget/packages/xunit.abstractions/2.0.3/lib/netstandard2.0/xunit.abstractions.dll"
       "-r:/Users/erik/.nuget/packages/xunit.assert/2.4.1/lib/netstandard1.1/xunit.assert.dll"
       "-r:/Users/erik/.nuget/packages/xunit.extensibility.core/2.4.1/lib/netstandard1.1/xunit.core.dll"
       "-r:/Users/erik/.nuget/packages/xunit.extensibility.execution/2.4.1/lib/netstandard1.1/xunit.execution.dotnet.dll"
       "--target:exe"
       "--warn:3"
       "--warnaserror:3239,76"
       "--fullpaths"
       "--flaterrors"
       "--highentropyva+"
       "--targetprofile:netcore"
       "--nocopyfsharpcore"
       "--deterministic+"
       "--simpleresolution"
       "/Users/erik/Code/exercism/fsharp-test-runner/test/Exercism.TestRunner.FSharp.IntegrationTests/Solutions/SingleTestThatPasses/obj/Debug/net5.0/.NETCoreApp,Version=v5.0.AssemblyAttributes.fs"
       "/Users/erik/Code/exercism/fsharp-test-runner/test/Exercism.TestRunner.FSharp.IntegrationTests/Solutions/SingleTestThatPasses/obj/Debug/net5.0/Fake.AssemblyInfo.fs"
       "/Users/erik/Code/exercism/fsharp-test-runner/test/Exercism.TestRunner.FSharp.IntegrationTests/Solutions/SingleTestThatPasses/Fake.fs"
       "/Users/erik/Code/exercism/fsharp-test-runner/test/Exercism.TestRunner.FSharp.IntegrationTests/Solutions/SingleTestThatPasses/FakeTests.fs"
       "/Users/erik/.nuget/packages/microsoft.net.test.sdk/16.9.4/build/netcoreapp2.1/Microsoft.NET.Test.Sdk.Program.fs" |]

// FILE: .NETCoreApp,Version=v5.0.AssemblyAttributes.fs
// namespace Microsoft.BuildSettings
//                 [<System.Runtime.Versioning.TargetFrameworkAttribute(".NETCoreApp,Version=v5.0", FrameworkDisplayName="")>]
//                 do ()

// FILE: Fake.AssemblyInfo.fs
// // <auto-generated>
// //     Generated by the FSharp WriteCodeFragment class.
// // </auto-generated>
// namespace FSharp

// open System
// open System.Reflection


// [<assembly: System.Reflection.AssemblyCompanyAttribute("Fake")>]
// [<assembly: System.Reflection.AssemblyConfigurationAttribute("Debug")>]
// [<assembly: System.Reflection.AssemblyFileVersionAttribute("1.0.0.0")>]
// [<assembly: System.Reflection.AssemblyInformationalVersionAttribute("1.0.0")>]
// [<assembly: System.Reflection.AssemblyProductAttribute("Fake")>]
// [<assembly: System.Reflection.AssemblyTitleAttribute("Fake")>]
// [<assembly: System.Reflection.AssemblyVersionAttribute("1.0.0.0")>]
// do()


[<EntryPoint>]
let main argv =
    //    let (errors, result) = checker.Compile(projectArgs) |> Async.RunSynchronously
//    [ for error in errors -> printfn "error: %A" error ] |> ignore

    Process
        .Start("/usr/local/share/dotnet/dotnet", projectArgs)
        .WaitForExit()

    Assembly.LoadFrom("Fake.dll")

    let tests = ConcurrentStack<TestInfo>()
    let finished = new ManualResetEventSlim()

    let runner =
        AssemblyRunner.WithoutAppDomain("Fake.dll")

    runner.OnTestFailed <- Action<TestFailedInfo>(tests.Push)
    runner.OnTestPassed <- Action<TestPassedInfo>(tests.Push)
    runner.OnExecutionComplete <- Action<ExecutionCompleteInfo>(fun info -> finished.Set())

    runner.Start()
    finished.Wait()

    for test in tests do
        printfn "%s" (test.GetType().Name)

    //    let wholeProjectResults = checker.ParseAndCheckProject(projectOptions) |> Async.RunSynchronously
//    [ for error in wholeProjectResults.Errors -> printfn "%A" error ]
//    [ for x in wholeProjectResults.AssemblySignature.Entities -> printfn "%A" x.DisplayName ]

    0
