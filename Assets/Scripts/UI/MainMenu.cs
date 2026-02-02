using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class MainMenu : MonoBehaviour
{
	[Header("Navigation")]
	[SerializeField] private Button firstSelectedButton;

	[Header("Selection Colors")]
	[SerializeField] private Color normalColor = new Color(1f, 1f, 1f, 1f);
	[SerializeField] private Color highlightedColor = new Color(1f, 1f, 1f, 1f);  // Beyaz tut (siyahlaşma yok)
	[SerializeField] private Color pressedColor = new Color(0.9f, 0.9f, 0.9f, 1f);   // Hafif koyu
	[SerializeField] private Color selectedColor = new Color(1f, 1f, 1f, 1f);        // Beyaz tut

	[Header("Button Animations")]
	[SerializeField] private bool autoAddAnimations = true;
	[SerializeField] private AudioClip buttonHoverSound;
	[SerializeField] private AudioClip buttonClickSound;

	[Header("Scene Transition")]
	[SerializeField] private float transitionDuration = 0.8f;
	[SerializeField] private RectTransform logoTransform;
	[SerializeField] private CanvasGroup canvasGroup; // Canvas'a CanvasGroup ekle

	private bool isTransitioning = false;
	private CanvasGroup fadeOverlay; // Otomatik oluşturulacak
	private Vector3 lastMousePosition; // Mouse hareket kontrolü için

	private void Start()
	{
		SetupButtons();
		if (autoAddAnimations)
		{
			SetupButtonAnimations();
		}
		SelectFirstButton();
		lastMousePosition = Input.mousePosition;
		
		// PlayerMovement varsa lockMovement'ı kapat
		var playerMovement = GetComponent<PlayerMovement>();
		if (playerMovement != null)
		{
			playerMovement.lockMovement = false;
		}

		Cursor.visible = true;
		Cursor.lockState = CursorLockMode.None;
		
		// Otomatik fade overlay oluştur
		CreateFadeOverlay();

		// DEBUG: Leaderboard'u göster
		LeaderboardEntry[] entries = LeaderboardData.GetEntries();
		for (int i = 0; i < entries.Length; i++)
		{
			Debug.Log($"Leaderboard {i + 1}: {entries[i].name} - {entries[i].score}");
		}
	}

	private void Update()
	{
		// Klavye navigasyonu kullanılırsa seçili buton yoksa ilkini seç
		if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.DownArrow) || 
		    Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.S))
		{
			if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == null)
			{
				SelectFirstButton();
			}
		}
	}
	
	private void CreateFadeOverlay()
	{
		// Canvas bul
		Canvas canvas = GetComponentInParent<Canvas>();
		if (canvas == null) canvas = FindObjectOfType<Canvas>();
		if (canvas == null) return;
		
		// Siyah overlay oluştur
		GameObject overlayObj = new GameObject("FadeOverlay");
		overlayObj.transform.SetParent(canvas.transform, false);
		overlayObj.transform.SetAsLastSibling(); // En üstte olsun
		
		// Image ekle
		var image = overlayObj.AddComponent<UnityEngine.UI.Image>();
		image.color = Color.black;
		image.raycastTarget = false;
		
		// Tüm ekranı kapla
		RectTransform rect = overlayObj.GetComponent<RectTransform>();
		rect.anchorMin = Vector2.zero;
		rect.anchorMax = Vector2.one;
		rect.offsetMin = Vector2.zero;
		rect.offsetMax = Vector2.zero;
		
		// CanvasGroup ekle
		fadeOverlay = overlayObj.AddComponent<CanvasGroup>();
		fadeOverlay.alpha = 0f;
		fadeOverlay.blocksRaycasts = false;
	}

	// Test için - Inspector'dan çağırabilirsin
	public void ClearLeaderboard()
	{
		LeaderboardData.ClearLeaderboard();
		Debug.Log("Leaderboard temizlendi!");
	}

	private void SetupButtons()
	{
		Button[] buttons = GetComponentsInChildren<Button>(true);

		foreach (Button btn in buttons)
		{
			ColorBlock colors = btn.colors;
			colors.normalColor = normalColor;
			colors.highlightedColor = highlightedColor;
			colors.pressedColor = pressedColor;
			colors.selectedColor = selectedColor;
			colors.colorMultiplier = 1f;
			btn.colors = colors;
		}
	}

	private void SetupButtonAnimations()
	{
		Button[] buttons = GetComponentsInChildren<Button>(true);

		foreach (Button btn in buttons)
		{
			// Eğer zaten animasyon yoksa ekle
			if (btn.GetComponent<MinimalistButtonAnimation>() == null && 
			    btn.GetComponent<MenuButtonAnimation>() == null)
			{
				// Minimalist animasyon ekle (premium görünüm)
				btn.gameObject.AddComponent<MinimalistButtonAnimation>();
			}
		}
	}

	private void SelectFirstButton()
	{
		if (firstSelectedButton != null && EventSystem.current != null)
		{
			EventSystem.current.SetSelectedGameObject(null);
			EventSystem.current.SetSelectedGameObject(firstSelectedButton.gameObject);
		}
	}

	public void PlayGame()
	{
		Debug.Log("PlayGame çağrıldı! isTransitioning: " + isTransitioning);
		
		if (isTransitioning) return;
		
		// ShopManager varsa reset et
		if (ShopManager.Instance != null)
		{
			ShopManager.Instance.ResetForNewGame();
		}
		
		Debug.Log("TransitionToLevel başlatılıyor...");
		StartCoroutine(TransitionToLevel("Level1"));
	}

	private IEnumerator TransitionToLevel(string sceneName)
	{
		isTransitioning = true;
		CheckpointData.ResetData();

		float elapsed = 0f;
		
		// Logo başlangıç değerleri
		Vector2 logoStartPos = logoTransform != null ? logoTransform.anchoredPosition : Vector2.zero;
		Vector3 logoStartScale = logoTransform != null ? logoTransform.localScale : Vector3.one;
		Vector2 logoEndPos = logoStartPos + new Vector2(0, 300f); // Yukarı kayar
		
		// CanvasGroup başlangıç
		float startAlpha = 1f;
		
		// Fade overlay başlangıçta şeffaf
		if (fadeOverlay != null)
		{
			fadeOverlay.alpha = 0f;
			fadeOverlay.blocksRaycasts = true;
		}

		while (elapsed < transitionDuration)
		{
			elapsed += Time.deltaTime;
			float t = elapsed / transitionDuration;
			
			// Ease out cubic - smooth
			float easeT = 1f - Mathf.Pow(1f - t, 3f);

			// Logo yukarı kayar ve büyür
			if (logoTransform != null)
			{
				logoTransform.anchoredPosition = Vector2.Lerp(logoStartPos, logoEndPos, easeT);
				logoTransform.localScale = Vector3.Lerp(logoStartScale, logoStartScale * 1.3f, easeT);
			}

			// Ekran soluklaşır (fade out)
			if (canvasGroup != null)
			{
				canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, easeT);
			}
			
			// Siyah overlay fade in (mavi ekranı engeller)
			if (fadeOverlay != null)
			{
				fadeOverlay.alpha = Mathf.Lerp(0f, 1f, easeT);
			}

			yield return null;
		}
		
		// Overlay'in tam siyah olduğundan emin ol
		if (fadeOverlay != null)
		{
			fadeOverlay.alpha = 1f;
		}

		// Sahneyi yükle
		var playerMovement = GetComponent<PlayerMovement>();
		if (playerMovement != null)
		{
			playerMovement.lockMovement = false;
		}
		
		SceneManager.LoadScene(sceneName);
	}

	public void QuitGame()
	{
		Application.Quit();
#if UNITY_EDITOR
		UnityEditor.EditorApplication.isPlaying = false;
#endif
	}
}

