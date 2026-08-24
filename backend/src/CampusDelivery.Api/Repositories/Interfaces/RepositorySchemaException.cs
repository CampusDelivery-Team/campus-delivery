namespace CampusDelivery.Api.Repositories.Interfaces;

public sealed class RepositorySchemaException(string message, Exception innerException)
    : Exception(message, innerException);
