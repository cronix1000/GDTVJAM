using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterFrameUI : MonoBehaviour
{
    public CharacterFrameUI Instance { get; private set; }
    
    public Image characterImage;
    public TextMeshProUGUI characterNameText;
    public TextMeshProUGUI speechText;
    public Button continueButton;
    public CanvasGroup canvasGroup;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    public void SetCharacterImage(Sprite sprite)
    {
        characterImage.sprite = sprite;
    }
    
    public void SetCharacterName(string name)
    {
        characterNameText.text = name;
    }
    
    // coroutine to display speech text like typewriter
    public void DisplaySpeech(string text, float charactersPerSecond = 30f)
    {
        StartCoroutine(TypeWriterEffect(text, speechText, charactersPerSecond));
    }
    
    IEnumerator TypeWriterEffect(string textToType, TextMeshProUGUI targetText, float charactersPerSecond)
    {
        targetText.text = "";
        if (charactersPerSecond <= 0) charactersPerSecond = 30f; // Default speed
        float delay = 1f / charactersPerSecond;
        foreach (char letter in textToType.ToCharArray())
        {
            targetText.text += letter;
            yield return new WaitForSeconds(delay);
        }
    }

    public void ShowCard(CharacterCardData stepStoryCardData)
    {
        Debug.Log("Showing Character Card");
        canvasGroup.alpha = 1f;
        if (stepStoryCardData)
        {
            SetCharacterImage(stepStoryCardData.characterPortrait);
            SetCharacterName(stepStoryCardData.characterName);
            DisplaySpeech(stepStoryCardData.dialogueText);
        }
        else
        {
            Debug.LogError("CharacterCardData is null!");
        }
    }

    public void HideCard()
    {
       canvasGroup.alpha = 0f;
    }
    
    // public void OnContinueButtonClicked()
    // {
    //     HideCard(); 
    //     GameStateManager.Instance.OnContinueButtonClicked();
    // }
}
