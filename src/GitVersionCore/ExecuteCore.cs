namespace GitVersion
{
    using System;
    using System.Linq;
    using GitVersion.Helpers;

    public static class ExecuteCore
    {
        public static VersionVariables ExecuteGitVersion(IFileSystem fileSystem, string targetUrl, string dynamicRepositoryLocation, Authentication authentication, string targetBranch, bool noFetch, string workingDirectory, string commitId)
        {
            // Normalise if we are running on build server
            var gitPreparer = new GitPreparer(targetUrl, dynamicRepositoryLocation, authentication, targetBranch, noFetch, workingDirectory);
            var applicableBuildServers = BuildServerList.GetApplicableBuildServers();
            var buildServer = applicableBuildServers.FirstOrDefault();
            var currentBranch = buildServer == null ? null : buildServer.GetCurrentBranch();
            if (!string.IsNullOrEmpty(currentBranch))
            {
                Logger.WriteInfo("Branch from build environment: " + currentBranch);
            }
            gitPreparer.Initialise(buildServer != null, currentBranch);
            var dotGitDirectory = gitPreparer.GetDotGitDirectory();
            var projectRoot = gitPreparer.GetProjectRootDirectory();
            Logger.WriteInfo(string.Format("Project root is: " + projectRoot));
            if (string.IsNullOrEmpty(dotGitDirectory) || string.IsNullOrEmpty(projectRoot))
            {
                // TODO Link to wiki article
                throw new Exception(string.Format("Failed to prepare or find the .git directory in path '{0}'.", workingDirectory));
            }
            VersionVariables variables;
            var versionFinder = new GitVersionFinder();
            var configuration = ConfigurationProvider.Provide(projectRoot, fileSystem);

            using (var repo = RepositoryLoader.GetRepo(dotGitDirectory))
            {
                var gitVersionContext = new GitVersionContext(repo, configuration, commitId: commitId);
                var semanticVersion = versionFinder.FindVersion(gitVersionContext);
                var config = gitVersionContext.Configuration;
                variables = VariableProvider.GetVariablesFor(semanticVersion, config.AssemblyVersioningScheme, config.VersioningMode, config.ContinuousDeploymentFallbackTag, gitVersionContext.IsCurrentCommitTagged);
            }

            return variables;
        }
    }
}