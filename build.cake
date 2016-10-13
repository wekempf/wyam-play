using System.Text.RegularExpressions;
using System.Collections.Generic;

#tool nuget:?package=Wyam&prerelease
#addin nuget:?package=Cake.Wyam&prerelease

//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");

//////////////////////////////////////////////////////////////////////
// PREPARATION
//////////////////////////////////////////////////////////////////////

var isLocal = BuildSystem.IsLocalBuild;
var isRunningOnUnix = IsRunningOnUnix();
var isRunningOnWindows = IsRunningOnWindows();
var isRunningOnAppVeyor = AppVeyor.IsRunningOnAppVeyor;
var isPullRequest = AppVeyor.Environment.PullRequest.IsPullRequest;

var repo = "git@github.com:wekempf/wyam-play.git";

//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////

Task("Build")
    .Does(() =>
    {
        Wyam(new WyamSettings
        {
            Recipe = "Blog",
            Theme = "CleanBlog"
        });        
    });
    
Task("Preview")
    .Does(() =>
    {
        Wyam(new WyamSettings
        {
            Recipe = "Blog",
            Theme = "CleanBlog",
            Preview = true,
            Watch = true
        });        
    });
    
//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("Default")
    .IsDependentOn("Preview");    
    
Task("AppVeyor")
    .IsDependentOn("Build")
    .Does(() => {
        //GitClone(repo, Directory("./clone"));

    });

Task("Test")
    .Does(() => {
        var origin = GitOrigin();
        GitClone(origin, "clone");
        GitCheckout("./clone", "gh-pages");
        DeleteFiles("./clone/*");
        var dirs = GetDirectories("./clone/*", d => !d.Path.FullPath.EndsWith(".git"));
        DeleteDirectories(dirs, true);
        CopyDirectory("./output", "./clone");
        GitCommitAndPush("./clone");
    });

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);

string GitOrigin()
{
    IEnumerable<string> output;
    StartProcess("git", new ProcessSettings {
        Arguments = "remote -v",
        RedirectStandardOutput = true
    }, out output);
    output = output ?? new string[0];
    var regex = new Regex(@"origin\s*(?<url>.*)\s*\(.*\)");
    var match = output
        .Select(s => regex.Match(s))
        .FirstOrDefault(m => m.Success);
    if (match == null) {
        throw new Exception("Unable to get Git remotes.");
    }
    return match.Groups["url"].Value;
}

void GitClone(string uri, string outputDirectory)
{
    StartProcess("git", new ProcessSettings {
        Arguments = "clone " + uri + " " + outputDirectory
    });
}

void GitCheckout(string repo, string branch) {
    StartProcess("git", new ProcessSettings {
        Arguments = "checkout -B " + branch,
        WorkingDirectory = repo
    });
}

void GitCommitAndPush(string repo) {
    StartProcess("git", new ProcessSettings {
        Arguments = "add .",
        WorkingDirectory = repo
    });
    StartProcess("git", new ProcessSettings {
        Arguments = "commit -m \"Deploying website\"",
        WorkingDirectory = repo
    });
    StartProcess("git", new ProcessSettings {
        Arguments = "push",
        WorkingDirectory = repo
    });
}