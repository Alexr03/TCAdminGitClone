using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using TCAdmin.GameHosting.SDK.Objects;
using TCAdmin.SDK.Web.MVC.Controllers;
using TCAdmin.Web.MVC;
using TCAdminGitClone.HttpResponses;

namespace TCAdminGitClone.Controllers
{
    [ExceptionHandler]
    [Authorize]
    public class GitController : BaseServiceController
    {
        [HttpPost]
        [ParentAction("Service", "Home")]
        public ActionResult Clone(int id, string target, string gitUrl, bool extract)
        {
            this.EnforceFeaturePermission("FileManager");
            try
            {
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
                var fileSystem = TCAdmin.SDK.Objects.Server.GetSelectedServer().FileSystemService;
                var gitCloneUrl = GetGitDownloadUrl(gitUrl);
                var saveTo = Path.Combine(service.WorkingDirectory, target, repoName);
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
                            responseText = "Error when extracting: " + e.Message
                        }, HttpStatusCode.InternalServerError);
                    }
                }

                return new JsonHttpStatusResult(new
                {
                    responseText = $"Successfully cloned <strong>{gitUrl}</strong>"
                }, HttpStatusCode.OK);
            }
            catch (Exception e)
            {
                return new JsonHttpStatusResult(new
                {
                    responseText = "An error occurred: " + e.Message
                }, HttpStatusCode.InternalServerError);
            }
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