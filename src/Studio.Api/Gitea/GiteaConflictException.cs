namespace Chatr.Studio.Api.Gitea;

public sealed class GiteaConflictException(string message) : Exception(message);
