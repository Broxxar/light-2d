using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CursorLight : MonoBehaviour
{
	public Camera MainCamera;
	private bool _visible = false;

	void Update()
	{
		if (Input.GetKeyDown(KeyCode.Escape))
			_visible = !_visible;

		Cursor.visible = _visible;

		Vector3 pos = MainCamera.ScreenToWorldPoint(Input.mousePosition);
		pos.z = 0;
		transform.position = pos;
	}

	void OnDisable()
	{
		Cursor.visible = true;
	}
}