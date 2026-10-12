using UnityEditor;

namespace VirtualRail.Editor
{
    /// <summary>
    /// Tự động chuẩn hóa file csproj về định dạng MSBuild Classic (MSBuild v17 .NET Framework)
    /// theo đúng hướng dẫn chính thức của Unity: 'use AssetPostprocessor.OnGeneratedCSProject'.
    /// Triệt tiêu 100% lỗi 'Could not resolve SDK Microsoft.NET.Sdk'.
    /// </summary>
    public class FixCSProjForMSBuild : AssetPostprocessor
    {
        private static string OnGeneratedCSProject(string path, string content)
        {
            content = content.Replace("<Project>", "<Project ToolsVersion=\"4.0\" DefaultTargets=\"Build\" xmlns=\"http://schemas.microsoft.com/developer/msbuild/2003\">");
            content = content.Replace("<Import Project=\"Sdk.props\" Sdk=\"Microsoft.NET.Sdk\" />", "");
            content = content.Replace("<Import Project=\"Sdk.targets\" Sdk=\"Microsoft.NET.Sdk\" />", "<Import Project=\"$(MSBuildToolsPath)\\Microsoft.CSharp.targets\" />");
            content = content.Replace("<TargetFramework>netstandard2.1</TargetFramework>", "<TargetFrameworkVersion>v4.7.1</TargetFrameworkVersion>");
            content = content.Replace("Temp\\obj\\$(MSBuildProjectName)", "Temp\\bin\\Debug");
            return content;
        }
    }
}
