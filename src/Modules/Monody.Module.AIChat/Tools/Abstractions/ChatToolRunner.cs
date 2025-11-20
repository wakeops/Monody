using System;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Monody.Module.AIChat.Tools.Attributes;
using OpenAI.Chat;

namespace Monody.Module.AIChat.Tools.Abstractions;

public abstract class ChatToolRunner
{
    public string Name => GetType().GetCustomAttribute<ChatToolRunnerAttribute>()?.Name ?? GetType().Name;

    public string SystemDescription => GetType().GetCustomAttribute<ChatToolRunnerAttribute>()?.SystemDescription;

    public ChatTool GetFunctionTool()
    {
        var type = GetType();

        var description = type.GetCustomAttribute<ChatToolFunctionAttribute>().Description;

        return ChatTool.CreateFunctionTool(Name, description, FunctionParametersBuilder());
    }

    protected virtual BinaryData FunctionParametersBuilder()
    {
        var attrs = GetType().GetCustomAttributes<ChatToolParameterAttribute>().ToList();

        if (attrs == null || attrs.Count == 0)
        {
            return null;
        }

        var sb = new StringBuilder();

        sb.Append("{ \"type\": \"object\", \"properties\": {");

        for (int i = 0; i < attrs.Count; i++)
        {
            var attr = attrs[i];

            if (i > 0)
            {
                sb.Append(',');
            }

            sb.Append($"\"{attr.Name}\": {{ \"type\": \"{attr.Type}\"");

            if (!string.IsNullOrWhiteSpace(attr.Description))
            {
                sb.Append($", \"description\": \"{attr.Description}\"");
            }

            sb.Append('}');
        }

        var requiredParams = string.Join(',', attrs.Where(a => a.Required).Select(a => $"\"{a.Name}\""));

        sb.Append("}, \"required\": [" + requiredParams + "]");

        return BinaryData.FromString(sb.ToString());
    }

    public abstract Task<ToolChatMessage> ExecuteAsync(ChatToolCall toolFn);
}

public abstract class ChatToolRunner<TParamsType> : ChatToolRunner
    where TParamsType: class
{
    protected override BinaryData FunctionParametersBuilder()
    {
        var paramsSchema = JsonSchemaGenerator.GenerateSchemaFor<TParamsType>();

        return BinaryData.FromString(paramsSchema);
    }

    public override async Task<ToolChatMessage> ExecuteAsync(ChatToolCall toolFn)
    {
        var argsJson = toolFn.FunctionArguments;
        var args = JsonSerializer.Deserialize<TParamsType>(argsJson);

        return await ExecuteAsync(toolFn, args);
    }

    public abstract Task<ToolChatMessage> ExecuteAsync(ChatToolCall toolFn, TParamsType args);
}
