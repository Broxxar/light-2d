using UnityEngine;
using System.Collections;

public class RuntimeObjectsDemo : MonoBehaviour
{
	public GameObject ObjectPrefab;

	void Update()
	{
		if (Input.GetMouseButtonDown(0))
		{
			GameObject newObj = Object.Instantiate<GameObject>(ObjectPrefab);
			newObj.transform.position = new Vector3(Random.Range(-7, 7), 6.5f);
			float s = Random.Range(0.4f, 0.8f);
			newObj.transform.localScale = new Vector3(s, s, s);
			newObj.GetComponent<Rigidbody2D>().angularVelocity = Random.Range(-180, 180);
		}
	}
}
