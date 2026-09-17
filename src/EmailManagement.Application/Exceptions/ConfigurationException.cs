namespace EmailManagement.Application.Exceptions;

public class ConfigurationException : Exception
{
    public ConfigurationException(string message) : base(message) { }
}