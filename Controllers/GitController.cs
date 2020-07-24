using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using TCAdmin.GameHosting.SDK.Objects;
using TCAdmin.Interfaces.Logging;
using TCAdmin.SDK.Web.MVC.Controllers;
using TCAdmin.Web.MVC;
using TCAdminGitClone.HttpResponses;

namespace TCAdminGitClone.Controllers
{
    [Authorize]
    public class GitController : BaseServiceController
    {
        [HttpPost]
        [ParentAction("Service", "Home")]
        public ActionResult Clone(int id, string target, string gitUrl, bool extract)
        {
            this.EnforceFeaturePermission("FileManager");
            var uriValid = Uri.TryCreate(gitUrl, UriKind.Absolute, out var gitUri) && gitUri != null &&
                           (gitUri.Scheme == Uri.UriSchemeHttp ||
                            gitUri.Scheme == Uri.UriSchemeHttps);
            if (!uriValid)
            {
                return new JsonHttpStatusResult(new
                {
                    responseText = "Invalid URL provided."
                }, HttpStatusCode.BadRequest);
            }

            var repoName = gitUri.Segments.LastOrDefault() + ".zip";
            var service = Service.GetSelectedService();
            var server = TCAdmin.SDK.Objects.Server.GetSelectedServer();
            var dirsec = service.GetDirectorySecurityForCurrentUser();
            var vdir = new TCAdmin.SDK.VirtualFileSystem.VirtualDirectory(server.OperatingSystem, dirsec);
            var fileSystem = server.FileSystemService;
            var gitCloneUrl = GetGitDownloadUrl(gitUrl);
            var saveTo = Path.Combine(service.WorkingDirectory, target, repoName);
            TCAdmin.SDK.LogManager.Write($"SaveTo: {saveTo}", LogType.Console);
            saveTo = vdir.CombineWithRealPhysicalPath(saveTo);
            TCAdmin.SDK.LogManager.Write($"SaveTo2: {saveTo}", LogType.Console);
            fileSystem.DownloadFile(saveTo, gitCloneUrl);

            if (extract)
            {
                try
                {
                    var serializedDirectorySecurity =
                        TCAdmin.SDK.Misc.ObjectXml.ObjectToXml(service.GetDirectorySecurityForCurrentUser());
                    fileSystem.Extract(saveTo, Path.GetDirectoryName(saveTo), serializedDirectorySecurity);
                }
                catch (Exception e)
                {
                    return new JsonHttpStatusResult(new
                    {
                        Message = "Error when extracting: " + e.Message
                    }, HttpStatusCode.InternalServerError);
                }
            }

            return new JsonHttpStatusResult(new
            {
                Message = $"Successfully cloned <strong>{gitUrl}</strong>"
            }, HttpStatusCode.OK);
        }

        private static string GetGitDownloadUrl(string gitUrl)
        {
            if (gitUrl.EndsWith(".zip"))
            {
                return gitUrl;
            }

            gitUrl = gitUrl.Replace(".git", "");
            gitUrl += "/archive/master.zip";
            return gitUrl;
        }
    }
}