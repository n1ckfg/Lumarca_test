using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class WavePattern : LumarcaLineRenderer {

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
		Vector3[] result = new Vector3[2];
		
		result[0] = UtilScript.CloneVec3(linePos);
		result[1] = UtilScript.CloneVec3(linePos);
		
		result[0].y +=  +10 + Mathf.Sin(counter + linePos.x/300f + linePos.z/300f) * 100;
		result[1].y +=  -10 + Mathf.Sin(counter + linePos.x/300f + linePos.z/300f) * 100;

		float col = UtilScript.Map(result[0].y, linePos.y - 100, linePos.y + 100, 0, 1);

		mat.color = new Color(col, 1 - col, 1, 1); 
		
		return result;
	}
}
