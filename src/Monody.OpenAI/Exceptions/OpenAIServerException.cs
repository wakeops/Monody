using System;

namespace Monody.OpenAI.Exceptions;

public class OpenAIServerException : Exception
{
    public OpenAIServerException(Exception ex) : base("Open AI client call failed", ex) { }
}
