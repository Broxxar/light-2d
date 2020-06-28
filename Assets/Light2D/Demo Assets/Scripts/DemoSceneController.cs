using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class DemoSceneController : MonoBehaviour
{
	private Image _fadeImage;
	private CanvasGroup _btnGroup;
	private bool _inProgress = false;

	void Start()
	{
		_fadeImage = transform.Find("FadePlane").GetComponent<Image>();
		_btnGroup = transform.Find("SceneControlButtons").GetComponent<CanvasGroup>();
		DontDestroyOnLoad(gameObject);
		StartCoroutine(InitAsync());
    }

	public void ChangeScenes(int sceneIndex)
	{
		if (!_inProgress)
			StartCoroutine(ChangeScenesAsync(sceneIndex));
	}

	IEnumerator InitAsync()
	{
		_inProgress = true;
		yield return new WaitForSeconds(1.0f);
		StartCoroutine(ChangeScenesAsync(1));
	}

	IEnumerator ChangeScenesAsync(int sceneIndex)
	{
		_inProgress = true;
		_fadeImage.enabled = true;

		for (float t = 0; t < 1.0f; t += Time.deltaTime * 3)
		{
			Color fade = _fadeImage.color;
			fade.a = t;
			_fadeImage.color = fade;
			yield return null;
		}

		SceneManager.LoadScene(sceneIndex);
		_btnGroup.alpha = 0.75f;

        for (float t = 0; t < 1.0f; t += Time.deltaTime * 3)
		{
			Color fade = _fadeImage.color;
			fade.a = 1 - t;
			_fadeImage.color = fade;
			yield return null;
		}

		_fadeImage.enabled = false;
		_inProgress = false;
	}
}
