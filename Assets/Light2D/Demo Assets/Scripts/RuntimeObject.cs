using UnityEngine;
using System.Collections;

public class RuntimeObject : MonoBehaviour
{
	void Awake()
	{
		Light2D.RegisterCollider(gameObject);
	}

	void Update()
	{
		if (transform.position.y < -10)
			Destroy(gameObject);
	}
}
