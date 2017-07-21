using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class WhaleWaves : LumarcaLineRenderer {

	// Use this for initialization
	void Start () {
		SetMaterial();
	}
	
	// Update is called once per frame
	void Update () {
		counter += Time.deltaTime;
	}

	public override Vector3[] GenerateLine(int lineNum, Vector3 linePos, 
	                                      float topX, float bottomX,
	                                      float topY, float bottomY,
	                                      float topZ, float bottomZ){
		List<Vector3> result = new List<Vector3>();
		
		Vector3 vec1 = UtilScript.CloneVec3(linePos);
		Vector3 vec2 = UtilScript.CloneVec3(linePos);
		
		vec1.y +=  300+5 + Mathf.Sin(counter + linePos.x/300f + linePos.z/300f) * 50;
		vec2.y +=  300-5 + Mathf.Sin(counter + linePos.x/300f + linePos.z/300f) * 50;
		
		result.Add(vec1);
		result.Add(vec2);
		
		float col = UtilScript.Map(vec1.y, linePos.y - 100, linePos.y + 100, 0, 1);
		
		//		Debug.Log(col);
		
		mat.color = new Color(0.3f, 0.75f, 1, 1); 
		
		return result.ToArray();
	}
}
