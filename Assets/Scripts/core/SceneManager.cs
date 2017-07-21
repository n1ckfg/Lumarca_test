using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SceneManager : MonoBehaviour {
	
	private static bool init = false;
	Dictionary<KeyCode, string> scenes;	

	// Use this for initialization
	void Start () {
		if(!init){
			init = true;
			DontDestroyOnLoad(gameObject);
		} else {
			Destroy(gameObject);
		}

		scenes = new Dictionary<KeyCode, string>();
		scenes.Add(KeyCode.Alpha1, "Lines-AnimatedMesh-Horse");
		scenes.Add(KeyCode.Alpha2, "Lines-LinePrograms-Waves");
		scenes.Add(KeyCode.Alpha3, "Lines-Mesh-Letters");
		scenes.Add(KeyCode.Alpha4, "Lines-Mesh-RotatingSpheres");
	}
	
	// Update is called once per frame
	void Update () {
		foreach(KeyCode key in scenes.Keys){
			if(Input.GetKeyDown(key)){
				Application.LoadLevel(scenes[key]);
			}
		}
	}
}
