
using UnityEngine;
using LLMUnity;
using System.Threading.Tasks;

public class LLMTester : MonoBehaviour
{
    [Header("LLM")]
    [SerializeField] private LLMAgent llmAgent;

    [Header("Prompt")]
    [TextArea(3, 6)]
    [SerializeField] private string question = "Hello! Tell me about yourself.";

    [ContextMenu("Ask LLM")]
    public async void Ask()
    {
        if (llmAgent == null)
        {
            Debug.LogError("LLMAgent is not assigned!", this);
            return;
        }

        if (string.IsNullOrWhiteSpace(question))
        {
            Debug.LogWarning("Question is empty.", this);
            return;
        }

        Debug.Log($"[LLM] Asking: {question}", this);

        try
        {
            string reply = await llmAgent.Chat(question);

            Debug.Log($"[LLM] Reply:\n{reply}", this);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[LLM] Request failed: {e}", this);
        }
    }

    public async Task<string> AskAsync(string prompt)
    {
        if (llmAgent == null)
        {
            Debug.LogError("LLMAgent is not assigned!", this);
            return null;
        }

        try
        {
            return await llmAgent.Chat(prompt);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[LLM] Request failed: {e}", this);
            return null;
        }
    }
}
