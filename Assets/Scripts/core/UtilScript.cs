using UnityEngine;
using System.Collections;
using SimpleJSON;
using System.IO;

public class UtilScript{
	
	const string JSON_X = "x";
	const string JSON_Y = "y";
	const string JSON_Z = "z";
	const string JSON_POS = "position";
	const string JSON_SCALE = "scale";

	public static Vector3 Vector3Mod(Vector3 initVec, float xMod, float yMod){
		return Vector3Mod (initVec, xMod, yMod, 0);
	}

	public static Vector3 Vector3Mod(Vector3 initVec, float xMod, float yMod, float zMod){
		Vector3 returnVec = new Vector3 (initVec.x + xMod,
		                                initVec.y + yMod,
		                                initVec.z + zMod);

		return returnVec;
	} 

	public static void WriteTransToFile(string fileName, Transform transform){

		JSONClass jTrans = new JSONClass();

		jTrans[JSON_POS] = JSON.Parse(Vector3ToJson(transform.position));
		jTrans[JSON_SCALE] = JSON.Parse(Vector3ToJson(transform.localScale));

		SaveStringToFile(fileName, jTrans.ToString());
	}

	public static Transform ReadTransformFromFile(string fileName){
		return null;
	}

	public static Vector3 ReadVector3FromFile(string fileName){
		string contents = ReadStringFromFile(fileName);
		Vector3 vec = JsonToVector3(contents);
		return vec;
	}

	public static void WriteVector3ToFile(string fileName, Vector3 vec){
		string contents = Vector3ToJson(vec);
		SaveStringToFile(fileName, contents);
	}

	public static Vector3 JsonToVector3(string json){
		Vector3 result;

		JSONNode node = JSON.Parse(json);

		float x = node[JSON_X].AsFloat;
		float y = node[JSON_Y].AsFloat;
		float z = node[JSON_Z].AsFloat;

		result = new Vector3(x, y, z);

		return result;
	}

	public static string Vector3ToJson(Vector3 vec){
		JSONClass node = new JSONClass();

		node[JSON_X].AsFloat = vec.x;
		node[JSON_Y].AsFloat = vec.y;
		node[JSON_Z].AsFloat = vec.z;

		return node.ToString();
	}

	public static string ReadStringFromFile(string fileName){
		//Open Stream
		StreamReader reader = new StreamReader(fileName);
		//Read string from file
		string contents = reader.ReadToEnd();
		//Close Stream
		reader.Close();
		//return string
		return contents;
	}

	public static void SaveStringToFile(string fileName, string contents){
		//Open Stream
		StreamWriter writer = new StreamWriter(fileName);
		//Write to Stream
		writer.Write(contents);
		//Close Stream
		writer.Close();
	}

	public static Vector3 CloneVec3(Vector3 v){
		return new Vector3(v.x, v.y, v.z);
	}
	
	
	public static Vector3 CloneVec3AndScale(Vector3 v, Vector3 s){
		return new Vector3(v.x * s.x, v.y * s.y, v.z * s.z);
	}

	public static float Map(float num, float oldMin, float oldMax, float newMin, float newMax){
		float oldRange = oldMax - oldMin;
		float newRange = newMax - newMin;
		
		float oldNum = num - oldMin;
		
		float percent = oldNum/oldRange;
		
		return (percent * newRange) + newMin;
	}
}
