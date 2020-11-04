using System;
using System.Collections.Generic;
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

            var repoName = GetFileName(gitUri.Segments.LastOrDefault());
            var service = Service.GetSelectedService();
            var game = TCAdmin.GameHosting.SDK.Objects.Game.GetSelectedGame();
            var server = TCAdmin.SDK.Objects.Server.GetSelectedServer();
            var fileSystem = server.FileSystemService;
            var gitCloneUrl = GetGitDownloadUrl(gitUri);
            string saveTo;
            if (!string.IsNullOrEmpty(game.Paths.RelativeUserFiles) &&
                TCAdmin.SDK.Web.Session.GetCurrentUser().UserType == TCAdmin.SDK.Objects.UserType.User)
            {
                saveTo = TCAdmin.SDK.Misc.FileSystem.CombinePath(server.OperatingSystem, service.RootDirectory,
                    game.Paths.RelativeUserFiles, target, repoName).TrimEnd('/', '\\');
            }
            else
            {
                saveTo = TCAdmin.SDK.Misc.FileSystem
                    .CombinePath(server.OperatingSystem, service.RootDirectory, target, repoName).TrimEnd('/', '\\');
            }

            fileSystem.DownloadFile(saveTo, gitCloneUrl.ToString());

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

        private static Uri GetGitDownloadUrl(Uri gitUrl)
        {
            var urlString = gitUrl.ToString();
            if (urlString.EndsWith(".zip") || urlString.EndsWith(".pk3") || urlString.EndsWith(".bsp") ||
                urlString.EndsWith(".ut2"))
            {
                return gitUrl;
            }

            urlString = urlString.Replace(".git", "");
            urlString += "/archive/master.zip";
            return new Uri(urlString);
        }

        private static string GetFileName(string file)
        {
            var extension = Path.GetExtension(file);
            if (string.IsNullOrEmpty(extension))
            {
                return file + ".zip";
            }

            if (extension == ".pk3")
            {
                return file + ".zip";
            }

            return file;
        }
    }
}