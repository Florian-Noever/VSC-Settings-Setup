namespace VSC_Settings_Setup.Services;

public sealed class ProcessFailedException(string message, Exception? innerException = null) : Exception(message, innerException);
