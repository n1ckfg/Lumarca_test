﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CenterOutwardWave : LumarcaLineRenderer {

	// Use this for initialization
	void Start () {
		SetMaterial();
		mat.color = new Color(0, 0, 1);
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
		
		Vector3 vec1 = UtilScript.CloneVec3(linePos);
		Vector3 vec2 = UtilScript.CloneVec3(linePos);
		
		float midX = ((topX + bottomX)/2);
		float midZ = ((topZ + bottomZ)/2);

		float xPos = linePos.x;
		float zPos = linePos.z;
		float hypot = Mathf.Sqrt(Mathf.Pow(xPos - midX, 2) + Mathf.Pow(zPos - midZ, 2));
		float hypotAdjusted = hypot - counter * 130f;
		float waveHeight = Mathf.Sin(hypotAdjusted/130f) * topY * .4f;

		result[0] = new Vector3(xPos, topY/2 + waveHeight, zPos);
		result[1] = new Vector3(xPos, bottomY, zPos);
		
		return result;
	}

}
