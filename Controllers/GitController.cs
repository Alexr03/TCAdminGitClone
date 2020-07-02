using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using TCAdmin.GameHosting.SDK.Objects;
using TCAdmin.SDK.Web.MVC.Controllers;
using TCAdminGitClone.HttpResponses;

namespace TCAdminGitClone.Controllers
{
    public class GitController : BaseServiceController
    {
        public ActionResult Clone(int id, string target, string gitUrl, bool extract)
        {
            this.EnforceFeaturePermission("FileManager");
            var uriValid = Uri.TryCreate(gitUrl, UriKind.Absolute, out var gitUri) && gitUri.Scheme == Uri.UriSchemeHttp ||
                           gitUri.Scheme == Uri.UriSchemeHttps;
            if (!uriValid)
            {
                return new JsonHttpStatusResult(new
                {
                    responseText = "Invalid URL provided."
                }, HttpStatusCode.BadRequest);
            }
            
            var repoName = gitUri.Segments.LastOrDefault() + ".zip";
            var service = Service.GetSelectedService();
            var fileSystem = new Server(service.ServerId).FileSystemService;
            var gitCloneUrl = GetGitDownloadUrl(gitUrl);
            var saveTo = Path.Combine(service.WorkingDirectory, target, repoName);
            fileSystem.DownloadFile(saveTo, gitCloneUrl);

            if (extract)
            {
                var serializedDirectorySecurity =
                    TCAdmin.SDK.Misc.ObjectXml.ObjectToXml(service.GetDirectorySecurityForCurrentUser());
                fileSystem.Extract(saveTo, Path.GetDirectoryName(saveTo), serializedDirectorySecurity);
            }

            return Json(new
            {
                responseText = $"Successfully cloned <strong>{gitUrl}</strong>"
            });
        }

        private static string GetGitDownloadUrl(string gitUrl)
        {
            if (gitUrl.EndsWith(".zip"))
            {
                return gitUrl;
            }
            
            gitUrl = gitUrl.Replace(".git", "");
            gitUrl += "archive/master.zip";

            return gitUrl;
        }
    }
}