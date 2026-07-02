namespace Chatr.Studio.Api.Gitea;

public sealed class GiteaNotFoundException(string message) : Exception(message);
