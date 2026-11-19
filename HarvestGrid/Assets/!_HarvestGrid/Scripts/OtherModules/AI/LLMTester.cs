
using GameJson;
using LgTyLib.Modules.DataPersistence;
using LLMUnity;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class LLMTester : MonoBehaviour, IDataPersistence
{
    [Header("LLM")]
    [SerializeField] private LLMAgent llmAgent;

    [Header("Prompt")]
    [TextArea(3, 6)]
    [SerializeField] private string question = "Hello! Tell me about yourself.";

    private bool isRequesting;

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

        await AskAsync(question);
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
            Debug.Log($"[LLM] Asking:\n{prompt}", this);

            string reply = await llmAgent.Chat(prompt);

            Debug.Log($"[LLM] Reply:\n{reply}", this);

            return reply;
        }
        catch (Exception e)
        {
            Debug.LogError($"[LLM] Request failed: {e}", this);
            return null;
        }
    }

    private async Task AskForRecommendationAsync(GameData gameData)
    {
        if (isRequesting)
        {
            Debug.LogWarning("[LLM] Already requesting a recommendation.", this);
            return;
        }

        if (llmAgent == null)
        {
            Debug.LogError("LLMAgent is not assigned!", this);
            return;
        }

        isRequesting = true;

        try
        {
            string prompt = BuildRecommendationPrompt(gameData);

            string reply = await llmAgent.Chat(prompt);

            Debug.Log($"[LLM] Player Recommendation:\n{reply}", this);
        }
        catch (Exception e)
        {
            Debug.LogError($"[LLM] Recommendation failed: {e}", this);
        }
        finally
        {
            isRequesting = false;
        }
    }

    private string BuildRecommendationPrompt(GameData gameData)
    {
        // Serialize player data.
        string userData = CleanJson.SimplifyObject(
            gameData,
            CleanJson.Options.ForAI()
        );

        // Serialize level data.
        string levelData = GameplayScene.Instance != null
            ? CleanJson.SimplifyObject(
                GameplayScene.Instance.LevelSO,
                CleanJson.Options.ForAI()
            )
            : "Level data unavailable.";

        // Collect available scenarios.
        StringBuilder scenariosBuilder = new StringBuilder();

        IReadOnlyList<AIScenario> scenarios =
            AIHelper.Instance != null
                ? AIHelper.Instance.Scenarios
                : null;

        if (scenarios != null)
        {
            foreach (AIScenario scenario in scenarios)
            {
                if (scenario == null)
                    continue;

                scenariosBuilder.AppendLine(
                    $"- {scenario.description}"
                );
            }
        }

        string scenariosText = scenariosBuilder.Length > 0
            ? scenariosBuilder.ToString()
            : "No scenarios available.";

        // Build the final prompt.
        StringBuilder prompt = new StringBuilder();

        prompt.AppendLine("You are an intelligent farming game assistant.");
        prompt.AppendLine("Your job is to analyze the player's current game state and recommend the best next action.");
        prompt.AppendLine();

        prompt.AppendLine("=== PLAYER DATA ===");
        prompt.AppendLine(userData);
        prompt.AppendLine();

        prompt.AppendLine("=== LEVEL DATA ===");
        prompt.AppendLine(levelData);
        prompt.AppendLine();

        prompt.AppendLine("=== AVAILABLE SCENARIOS ===");
        prompt.AppendLine(scenariosText);
        prompt.AppendLine();

        prompt.AppendLine("=== INSTRUCTIONS ===");
        prompt.AppendLine("Analyze all provided information before making a recommendation.");
        prompt.AppendLine("Consider the following:");
        prompt.AppendLine("1. Current player resources and money.");
        prompt.AppendLine("2. Farm slots, soil quality, moisture and planted crops.");
        prompt.AppendLine("3. Crop growth stages and their resource requirements.");
        prompt.AppendLine("4. Available seeds, items and inventory.");
        prompt.AppendLine("5. Level objectives and progression.");
        prompt.AppendLine("6. Available scenarios and their potential benefits.");
        prompt.AppendLine();
        prompt.AppendLine("Recommend the most useful action the player should take next.");
        prompt.AppendLine("Prioritize actions that help complete the level efficiently.");
        prompt.AppendLine("Do not recommend actions that the player cannot afford or perform.");
        prompt.AppendLine("Do not invent game mechanics or unavailable items.");
        prompt.AppendLine("If important information is missing, state your assumptions.");
        prompt.AppendLine();
        prompt.AppendLine("=== RESPONSE FORMAT ===");
        prompt.AppendLine("Current situation: Briefly summarize the player's state.");
        prompt.AppendLine("Recommended action: Give one specific action.");
        prompt.AppendLine("Reason: Explain why this is the best action.");
        prompt.AppendLine("Alternative: Give one alternative if applicable.");
        prompt.AppendLine("Keep the response concise and easy for a player to understand.");

        return prompt.ToString();
    }

    public void LoadGame(GameData gameData)
    {
    }

    public void SaveGame(ref GameData gameData)
    {
        // Start the request without blocking the save operation.
        _ = AskForRecommendationAsync(gameData);
    }
}
