using System;

namespace Monody.AI.Provider.OpenAI.Exceptions;

public class OpenAIServerException : Exception
{
    public OpenAIServerException(Exception ex) : base("Open AI client call failed", ex) { }
}
