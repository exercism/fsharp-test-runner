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

    [|  @"fsc.exe"
        @"-o:Fake.dll"
        @"-g"
        @"--debug:portable"
        @"--noframework"
        @"--define:TRACE"
        @"--define:DEBUG"
        @"--define:NET"
        @"--define:NET5_0"
        @"--define:NETCOREAPP"
        @"--define:NET5_0_OR_GREATER"
        @"--define:NETCOREAPP1_0_OR_GREATER"
        @"--define:NETCOREAPP1_1_OR_GREATER"
        @"--define:NETCOREAPP2_0_OR_GREATER"
        @"--define:NETCOREAPP2_1_OR_GREATER"
        @"--define:NETCOREAPP2_2_OR_GREATER"
        @"--define:NETCOREAPP3_0_OR_GREATER"
        @"--define:NETCOREAPP3_1_OR_GREATER"
        @"--optimize-"
        @"--tailcalls-"
        @"-r:C:\Users\Erik\.nuget\packages\fsharp.core\5.0.0\lib\netstandard2.0\FSharp.Core.dll"
        @"-r:C:\Users\Erik\.nuget\packages\fsunit.xunit\4.0.4\lib\netstandard2.0\FsUnit.Xunit.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\Microsoft.CSharp.dll"
        @"-r:C:\Users\Erik\.nuget\packages\microsoft.testplatform.testhost\16.9.4\lib\netcoreapp2.1\Microsoft.TestPlatform.CommunicationUtilities.dll"
        @"-r:C:\Users\Erik\.nuget\packages\microsoft.testplatform.testhost\16.9.4\lib\netcoreapp2.1\Microsoft.TestPlatform.CoreUtilities.dll"
        @"-r:C:\Users\Erik\.nuget\packages\microsoft.testplatform.testhost\16.9.4\lib\netcoreapp2.1\Microsoft.TestPlatform.CrossPlatEngine.dll"
        @"-r:C:\Users\Erik\.nuget\packages\microsoft.testplatform.testhost\16.9.4\lib\netcoreapp2.1\Microsoft.TestPlatform.PlatformAbstractions.dll"
        @"-r:C:\Users\Erik\.nuget\packages\microsoft.testplatform.testhost\16.9.4\lib\netcoreapp2.1\Microsoft.TestPlatform.Utilities.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\Microsoft.VisualBasic.Core.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\Microsoft.VisualBasic.dll"
        @"-r:C:\Users\Erik\.nuget\packages\microsoft.codecoverage\16.9.4\lib\netcoreapp1.0\Microsoft.VisualStudio.CodeCoverage.Shim.dll"
        @"-r:C:\Users\Erik\.nuget\packages\microsoft.testplatform.testhost\16.9.4\lib\netcoreapp2.1\Microsoft.VisualStudio.TestPlatform.Common.dll"
        @"-r:C:\Users\Erik\.nuget\packages\microsoft.testplatform.testhost\16.9.4\lib\netcoreapp2.1\Microsoft.VisualStudio.TestPlatform.ObjectModel.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\Microsoft.Win32.Primitives.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\mscorlib.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\netstandard.dll"
        @"-r:C:\Users\Erik\.nuget\packages\newtonsoft.json\9.0.1\lib\netstandard1.0\Newtonsoft.Json.dll"
        @"-r:C:\Users\Erik\.nuget\packages\nhamcrest\2.0.1\lib\netstandard1.5\NHamcrest.dll"
        @"-r:C:\Users\Erik\.nuget\packages\nuget.frameworks\5.0.0\lib\netstandard2.0\NuGet.Frameworks.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.AppContext.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Buffers.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Collections.Concurrent.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Collections.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Collections.Immutable.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Collections.NonGeneric.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Collections.Specialized.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.ComponentModel.Annotations.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.ComponentModel.DataAnnotations.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.ComponentModel.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.ComponentModel.EventBasedAsync.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.ComponentModel.Primitives.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.ComponentModel.TypeConverter.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Configuration.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Console.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Core.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Data.Common.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Data.DataSetExtensions.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Data.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Diagnostics.Contracts.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Diagnostics.Debug.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Diagnostics.DiagnosticSource.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Diagnostics.FileVersionInfo.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Diagnostics.Process.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Diagnostics.StackTrace.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Diagnostics.TextWriterTraceListener.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Diagnostics.Tools.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Diagnostics.TraceSource.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Diagnostics.Tracing.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Drawing.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Drawing.Primitives.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Dynamic.Runtime.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Formats.Asn1.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Globalization.Calendars.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Globalization.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Globalization.Extensions.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.IO.Compression.Brotli.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.IO.Compression.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.IO.Compression.FileSystem.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.IO.Compression.ZipFile.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.IO.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.IO.FileSystem.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.IO.FileSystem.DriveInfo.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.IO.FileSystem.Primitives.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.IO.FileSystem.Watcher.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.IO.IsolatedStorage.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.IO.MemoryMappedFiles.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.IO.Pipes.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.IO.UnmanagedMemoryStream.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Linq.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Linq.Expressions.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Linq.Parallel.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Linq.Queryable.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Memory.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Net.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Net.Http.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Net.Http.Json.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Net.HttpListener.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Net.Mail.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Net.NameResolution.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Net.NetworkInformation.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Net.Ping.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Net.Primitives.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Net.Requests.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Net.Security.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Net.ServicePoint.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Net.Sockets.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Net.WebClient.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Net.WebHeaderCollection.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Net.WebProxy.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Net.WebSockets.Client.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Net.WebSockets.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Numerics.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Numerics.Vectors.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.ObjectModel.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Reflection.DispatchProxy.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Reflection.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Reflection.Emit.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Reflection.Emit.ILGeneration.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Reflection.Emit.Lightweight.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Reflection.Extensions.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Reflection.Metadata.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Reflection.Primitives.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Reflection.TypeExtensions.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Resources.Reader.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Resources.ResourceManager.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Resources.Writer.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Runtime.CompilerServices.Unsafe.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Runtime.CompilerServices.VisualC.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Runtime.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Runtime.Extensions.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Runtime.Handles.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Runtime.InteropServices.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Runtime.InteropServices.RuntimeInformation.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Runtime.Intrinsics.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Runtime.Loader.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Runtime.Numerics.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Runtime.Serialization.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Runtime.Serialization.Formatters.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Runtime.Serialization.Json.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Runtime.Serialization.Primitives.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Runtime.Serialization.Xml.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Security.Claims.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Security.Cryptography.Algorithms.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Security.Cryptography.Csp.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Security.Cryptography.Encoding.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Security.Cryptography.Primitives.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Security.Cryptography.X509Certificates.dll"     
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Security.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Security.Principal.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Security.SecureString.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.ServiceModel.Web.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.ServiceProcess.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Text.Encoding.CodePages.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Text.Encoding.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Text.Encoding.Extensions.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Text.Encodings.Web.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Text.Json.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Text.RegularExpressions.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Threading.Channels.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Threading.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Threading.Overlapped.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Threading.Tasks.Dataflow.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Threading.Tasks.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Threading.Tasks.Extensions.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Threading.Tasks.Parallel.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Threading.Thread.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Threading.ThreadPool.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Threading.Timer.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Transactions.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Transactions.Local.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.ValueTuple.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Web.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Web.HttpUtility.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Windows.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Xml.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Xml.Linq.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Xml.ReaderWriter.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Xml.Serialization.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Xml.XDocument.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Xml.XmlDocument.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Xml.XmlSerializer.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Xml.XPath.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Xml.XPath.XDocument.dll"
        @"-r:C:\Users\Erik\.nuget\packages\microsoft.testplatform.testhost\16.9.4\lib\netcoreapp2.1\testhost.dll"
        @"-r:C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\WindowsBase.dll"
        @"-r:C:\Users\Erik\.nuget\packages\xunit.abstractions\2.0.3\lib\netstandard2.0\xunit.abstractions.dll"
        @"-r:C:\Users\Erik\.nuget\packages\xunit.assert\2.4.1\lib\netstandard1.1\xunit.assert.dll"
        @"-r:C:\Users\Erik\.nuget\packages\xunit.extensibility.core\2.4.1\lib\netstandard1.1\xunit.core.dll"
        @"-r:C:\Users\Erik\.nuget\packages\xunit.extensibility.execution\2.4.1\lib\netstandard1.1\xunit.execution.dotnet.dll"
        @"--target:exe"
        @"--warn:3"
        @"--warnaserror:3239,76"
        @"--fullpaths"
        @"--flaterrors"
        @"--highentropyva+"
        @"--targetprofile:netcore"
        @"--nocopyfsharpcore"
        @"--deterministic+"
        @"--simpleresolution"
        "--nowin32manifest" 
        @"C:\Code\exercism\fsharp-test-runner\test\Exercism.TestRunner.FSharp.IntegrationTests\Solutions\MultipleTestsWithSingleFail\obj\Debug\net5.0\.NETCoreApp,Version=v5.0.AssemblyAttributes.fs"
        @"C:\Code\exercism\fsharp-test-runner\test\Exercism.TestRunner.FSharp.IntegrationTests\Solutions\MultipleTestsWithSingleFail\obj\Debug\net5.0\Fake.AssemblyInfo.fs"
        @"C:\Code\exercism\fsharp-test-runner\test\Exercism.TestRunner.FSharp.IntegrationTests\Solutions\MultipleTestsWithSingleFail\Fake.fs"
        @"C:\Code\exercism\fsharp-test-runner\test\Exercism.TestRunner.FSharp.IntegrationTests\Solutions\MultipleTestsWithSingleFail\FakeTests.fs"
     |]

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


// https://github.com/dotnet/fsharp/issues/1024
// https://github.com/fsharp/FSharp.Compiler.Service/issues/755
// FILE: default.win32manifest
//<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
//<assembly xmlns="urn:schemas-microsoft-com:asm.v1" manifestVersion="1.0">
//  <assemblyIdentity version="1.0.0.0" name="MyApplication.app"/>
//  <trustInfo xmlns="urn:schemas-microsoft-com:asm.v2">
//    <security>
//      <requestedPrivileges xmlns="urn:schemas-microsoft-com:asm.v3">
//        <requestedExecutionLevel level="asInvoker" uiAccess="false"/>
//      </requestedPrivileges>
//    </security>
//  </trustInfo>
//</assembly>

[<EntryPoint>]
let main argv =
    let (errors, result) = checker.Compile(projectArgs) |> Async.RunSynchronously
    [ for error in errors -> printfn "error: %A" error ] |> ignore

//    Process
//        .Start("/usr/local/share/dotnet/dotnet", projectArgs)
//        .WaitForExit()

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
