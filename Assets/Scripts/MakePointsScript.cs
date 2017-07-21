using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using SimpleJSON;

public class MakePointsScript : MonoBehaviour {

	Camera cam;
	CameraFrustumScript cfs;

	public bool part1 = true;

	public int numDepths = 10;
	public int dotSize = 4;
	public Vector3[] front;
	public Vector3[] back;

	public bool drawCube;
	public GameObject dotHolder;
	
	float totalWidth;
	float totalHeight;
	float totalDepth;
	
	const string SHADER_MODE = "_Mode";

	public enum LUMARCA_MODE {Dots, VLines};

	public LUMARCA_MODE lumarcaMode;
	
	public Material mat;
	
	public List<Vector3> lines;
	public List<GameObject> lineObjects;
	public List<int> depthList;

	public static float tolerance = 50;
	List<int> bag;
	
	Vector3 camPos;

	// Use this for initialization
	void Start () {

//		float input = 237.5654665f;
//		
//		float part1 = GetColorPart1(input, 0, 1024);
//		float part2 = GetColorPart2(input, 0, 1024);
//
//		Debug.Log ("MAP1: " + part1);
//		Debug.Log ("MAP2: " + part2);
//		Debug.Log ("Convert: " + PartsToFloat(part1, part2, 0, 1024));

		cam = Camera.main;

		cfs = cam.GetComponent<CameraFrustumScript>();

		front = cfs.GetFrontPlane();
		back = new Vector3[4];

		for(int i = 0; i < front.Length; i++){
			back[i] = UtilScript.CloneVec3(front[i]);
			back[i].z = cam.farClipPlane - cam.nearClipPlane;
		}

		totalWidth = front[1].x - front[0].x;
		totalHeight = front[2].y - front[0].y;
		totalDepth = back[0].z  - front[0].z;

		camPos = cam.transform.position;

		if(lumarcaMode == LUMARCA_MODE.Dots){
			GenerateDots();
		} else {
			GenerateLines();
			MakeDiscreetAndCutOutsideOfCube();
		}

		SetColorToPart();
	}

	List<int> MakeBag(){
		List<int> result = new List<int>();

		for(int i = 0; i < numDepths; i++){
			result.Add (i);
		}

		return result;
	}

	bool TooClose(Vector3 coord1, Vector3 coord2){
		
		if(Vector3.Distance(coord1, coord2) < tolerance){
			return true;
		}
		
		return false;
	}

	private bool ValidLine(Vector3 pos){
		bool result = true;
		
		for(int i = 0 ; i < lines.Count; i++){
			if(TooClose(lines[i], pos)){
				result = false;
				break;
			}
		}
		
		return result;
	}


	Vector3 GetNewLinePos(int depthPos, float x, Vector3 camPos, float depthIncrement){
//		int i = (int)(Random.Range(0, bag.Count));
		
		int depthLevel = bag[depthPos];
//		bag.RemoveAt(i);
		
		float z = depthLevel * depthIncrement;
		
		Vector3 lineSpotAtFront = new Vector3(x, totalHeight/2, front[0].z);
		
		Vector3 dir =  lineSpotAtFront - camPos;
		dir.Normalize();
		
		float distToSpot = (z - camPos.z)/dir.z;
		
		float zMult = distToSpot;
		
		return new Vector3(dir.x * zMult, totalHeight/2, z);
	}

	void GenerateLines(){
		float depthIncrement = totalDepth/(numDepths - 1);
		
		dotHolder = new GameObject("DotHolder");
		
		GameObject[] depthHolders = new GameObject[numDepths];
		
		for(int i = 0; i < numDepths; i++){
			GameObject go = new GameObject("Depth " + i);
			go.transform.parent = dotHolder.transform;
			depthHolders[i] = go;
		}

		bag = MakeBag();
		
		lines = new List<Vector3>();
		lineObjects = new List<GameObject>();

		int index = 0;

		int maxAttempts = 125;

		for(float x = front[0].x + dotSize/2; x < front[1].x; x += dotSize){
			int attempts = 0;

			int depthPos = (int)(Random.Range(0, bag.Count));
			
			Vector3 pos = GetNewLinePos(depthPos, x, camPos, depthIncrement);

			while(!ValidLine(pos) && attempts < maxAttempts){
				depthPos = (int)(Random.Range(0, bag.Count));
				pos = GetNewLinePos(depthPos, x, camPos, depthIncrement);

				attempts++;
			}

			if(attempts == maxAttempts){
				tolerance -= 1;

				Application.LoadLevel(Application.loadedLevel);
			}

			bag.RemoveAt(depthPos);
			
//			if(InsideCube(pos)){
				lines.Add(pos);

				index++;
					
				GameObject q = Instantiate(Resources.Load("Quad")) as GameObject;
				q.transform.parent = depthHolders[depthPos].transform;
				q.transform.localScale = new Vector3(dotSize/2, totalHeight, dotSize/2);
				q.transform.position = pos;

				lineObjects.Add(q);
//			}

//			Debug.Log("POS: " + pos);
		}
		
		Debug.Log ("tolerance: " + tolerance);
		Debug.Log ("Num Lines: " + index);
//		Debug.Log ("lineObjects[0]: " + lineObjects[0].transform.position);
//		Debug.Log ("lines[0]: " + lines[0]);
	}

	void CutOutSideOfCube(){
		
		Debug.Log("objects Length: " + lineObjects.Count);
		
		List<Vector3> removeVecs = new List<Vector3>();
		List<GameObject> removeGameObjects = new List<GameObject>();

		for(int i = 0; i < lines.Count; i++){
			Vector3 pos = lines[i];

			if(!InsideCube(pos)){
				removeVecs.Add(pos);
			}
			
			if(!InsideCube(lineObjects[i].transform.position)){
				removeGameObjects.Add(lineObjects[i]);
			}
		}
		
		Debug.Log("removeVecs Length: " + removeVecs.Count);
		Debug.Log("GameObject Length: " + removeGameObjects.Count);

		for(int i = 0; i < removeVecs.Count; i++){
			Vector3 pos = removeVecs[i];
			lines.Remove(pos);
			
			GameObject o = removeGameObjects[i];
			lineObjects.Remove(o);
			Destroy(o);
		}
		
		Debug.Log("Lines Length: " + lines.Count);
		Debug.Log("GameObject Length: " + lineObjects.Count);
	}

	void MakeDiscreet(){
		makeDepthList();
		
		for(int i = 0; i < lines.Count; i++){
			
			int whichString = depthList[i];
			
			Vector3 line = lines[whichString];

//			Debug.Log("i: " + i);

			lines[whichString] = GetPosAtNewZForLines(line, Mathf.Lerp(front[0].z, 
			                                                   back[0].z, 
			                                                   i/(float)lines.Count));

			lineObjects[whichString].transform.position = lines[whichString];
		}
	}

	void MakeDiscreetAndCutOutsideOfCube(){
//		Debug.Log("Start count:" + lines.Count);

		int numLines = lines.Count;
		MakeDiscreet();

//		float pDepth = 0;

//		for(int i = 0; i < lines.Count; i++){
//			Vector3 pos = lines[depthList[i]];
//			Debug.Log("Depth: " + (pDepth - pos.z));
//			pDepth = pos.z;
//		}

		CutOutSideOfCube();
		
		while(numLines !=  lines.Count){
			MakeDiscreet();
			numLines = lines.Count;
			CutOutSideOfCube();
		}

//		float prevZ = 0;

//		foreach(int i in depthList){
//			Vector3 vec = lines[i];
//			prevZ = vec.z;
//		}

//		Debug.Log("End count:" + lines.Count);
	}

	private void makeDepthList(){
		depthList = new List<int>(); 
		depthList.Add(0);
		
		for(int i = 1; i < lines.Count; i++){
			
			Vector3 current = lines[i];
			bool inserted = false;
			
			for(int c = 0; c < i; c++){
				Vector3 prev = lines[depthList[c]];
				if(current.z > prev.z){
					depthList.Insert(c, i);
					inserted = true;
					break;
				}
			}
			
			if(!inserted){
				depthList.Add(i);
			}
		}

		depthList.Reverse();
	}

	
	Vector3 GetPosAtNewZ(Vector3 pos, float newZ){
		
		Vector3 newPos = pos - camPos;
		
		newPos.Normalize();
		
		float f = ((newZ - camPos.z)/newPos.z);
		
		newPos = camPos + newPos * f;
		
		return newPos;
	}

	//NOTE: DO NOT USE FOR DOTS
	Vector3 GetPosAtNewZForLines(Vector3 pos, float newZ){

		Vector3 newPos = GetPosAtNewZ(pos, newZ);
	
		newPos.y = pos.y;

		return newPos;
	}

	void GenerateDots(){
		float depthIncrement = totalDepth/(numDepths - 1);
		
		Debug.Log("frontY: " + front[2].y/dotSize);
		
		int dotsPerLine = (int)front[2].y/dotSize;
		
		dotHolder = new GameObject("DotHolder");
		
		GameObject[] depthHolders = new GameObject[numDepths];
		
		for(int i = 0; i < numDepths; i++){
			GameObject go = new GameObject("Depth " + i);
			go.transform.parent = dotHolder.transform;
			depthHolders[i] = go;
		}
		
		Vector3 camPos = cam.transform.position;

		int depthLevel = 0;
		
		for(float x = front[0].x + dotSize/2; x < front[1].x; x += dotSize){
			depthLevel = Random.Range(0, numDepths);
			for(float y = front[0].y + dotSize/2; y < front[2].y; y += dotSize){
				depthLevel = Random.Range(0, numDepths);
				//depthLevel++;
				
				if(depthLevel >= numDepths){
					depthLevel = 0;
				}
				
				//				depthLevel = bag[0];
				//				bag.RemoveAt(0);
				
				float z = depthLevel * depthIncrement;
				
				Vector3 dir =  new Vector3(x, y, front[0].z) - camPos;
				
				dir.Normalize();
				
				float zMult = (z - camPos.z)/dir.z;
				
				Vector3 pos = new Vector3(dir.x * zMult, dir.y * zMult, z);
				
				if(InsideCube(pos)){
					GameObject q = Instantiate(Resources.Load("Quad")) as GameObject;
					q.transform.parent = depthHolders[depthLevel].transform;
					q.transform.localScale = new Vector3(dotSize/2, dotSize/2, dotSize/2);
					q.transform.position = pos;
				}
			}
		}
	}

	void SetColorToPart(){
	
		Color c = new Color(0, 1, 0, 1);
		
		for(int i = 0; i < dotHolder.transform.childCount; i++){
			GameObject depth = dotHolder.transform.GetChild(i).gameObject;

//			Debug.Log("Depth: " + i + " Num Dots: " + depth.transform.childCount);

			for(int j = 0; j < depth.transform.childCount; j++){
				GameObject dot = depth.transform.GetChild(j).gameObject;

				Vector3 pos = dot.transform.position;

				if(part1){
					c.r = GetColorPart1(pos.x, front[0].x, front[1].x);
					c.g = GetColorPart1(pos.y, front[0].y, front[2].y);
					c.b = GetColorPart1(pos.z, front[0].z, back[1].z);
				} else {
//					c.r = GetColorPart2(pos.x + totalWidth);
//					c.g = GetColorPart2(pos.y);
//					c.b = GetColorPart2(pos.z);
				}
				
				if(part1){
					dot.GetComponent<MeshRenderer>().material.SetInt(SHADER_MODE, 0);
				} else {
					dot.GetComponent<MeshRenderer>().material.SetInt(SHADER_MODE, 1);
				}
				
	//			c.r = 0;
	//			c.g = 0;
				c.a = 1;
				
//				Debug.Log(c.r + "x" + c.g + "x" + c.b);
////				Debug.Log(c.r + "x" + c.g + "x" + c.b);
////				Debug.Log("return: " + PartsToFloat(GetColorPart1(pos.z), GetColorPart2(pos.z)));
//				Debug.Log("1: " +  GetColorPart1(Mathf.Abs(pos.x)));
//				Debug.Log("2: " +  GetColorPart2(Mathf.Abs(pos.x)));
//				Debug.Log("return: " + PartsToFloat(GetColorPart1(Mathf.Abs(pos.x)), GetColorPart2(Mathf.Abs(pos.x))));
//				
//				float cx1 = GetColorPart1(Mathf.Abs(pos.x));
//				float cx2 = GetColorPart2(Mathf.Abs(pos.x));
//				
//				Debug.Log("org: " + (PartsToFloat(cx1, cx2)));
//
//				dot.GetComponent<MeshRenderer>().material.color = c;
			}
		}
	}

	float Map(float num, float oldMin, float oldMax, float newMin, float newMax){
		float oldRange = oldMax - oldMin;
		float newRange = newMax - newMin;

		float oldNum = num - oldMin;

		float percent = oldNum/oldRange;

		return (percent * newRange) + newMin;
	}

	float GetColorPart1(float f, float min, float max){
		float val = Map(f, min, max, 0, 255); 

		float d = (Mathf.Floor(val)/255f);

		return d;
	}

	float GetColorPart2(float f, float min, float max){
		float part1 = GetColorPart1(f, min, max);

		float val = Map(f, min, max, 0, 255)/255f; 

		val = val - part1;

		return val * 255;
	}

	float PartsToFloat(float i1, float i2, float min, float max){
		float part1 = Map(i1, 0, 1, min, max);
		float part2 = Map(i2/255f, 0, 1, min, max);

		return part1 + part2;
	} 


	bool InsideCube(Vector3 pos){
		if(pos.y < front[2].y &&
		   pos.x > front[0].x&&
		   pos.x < front[1].x){
			return true;
		}

		return false;
	}
	
	// Update is called once per frame
	void Update () {
		if(Input.GetKeyDown(KeyCode.Space)){
			
			Debug.Log ("tolerance: " + tolerance);
			Debug.Log("End count:" + lines.Count);

			part1 = !part1;
			SetColorToPart();

			int part = 2;
			if(part1)
				part = 1;

			StartCoroutine(UploadPNG("Lumarca" + 
			                         Camera.main.pixelWidth + "x" + 
			                         Camera.main.pixelHeight + 
			                         "_DotSize" + dotSize + 
			                         "Depth" + numDepths +
			                         "Part" + part));

			JSONArray jPositions = new JSONArray();

			for(int i = 0; i < lines.Count; i++){
				jPositions[i] = UtilScript.Vector3ToJson(lines[i]);
			}

			BinaryWriter writer = new BinaryWriter(File.Open("positions.json", FileMode.Create));
			jPositions.Serialize(writer);
			writer.Close();
			
			BinaryReader reader = new BinaryReader(File.Open("positions.json", FileMode.Open));
			JSONNode ja = SimpleJSON.JSONNode.Deserialize(reader);

			Debug.Log ("Vec3:  " + ja[0].ToString());
			Debug.Log ("Vec3:  " +  JSON.Parse(ja[0])["x"]);

			for(int i = 0; i < ja.Count; i++){
				Debug.Log ("jn:  " + i + " : " + JSON.Parse(ja[i])["x"]);
			}
		}
		
//		DrawBox();
	}

	
	void OnPostRender() {
		if(drawCube)
			DrawBox();
	}

	void OnDrawGizmos(){
		if(drawCube)
			DrawBox();
	}

	void DrawBox(){
		if (!mat)
		{
			// Unity has a built-in shader that is useful for drawing
			// simple colored things. In this case, we just want to use
			// a blend mode that inverts destination colors.			
			var shader = Shader.Find ("Hidden/Internal-Colored");
			mat = new Material (shader);
			mat.hideFlags = HideFlags.HideAndDontSave;
			// Set blend mode to invert destination colors.
			mat.SetInt ("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusDstColor);
			mat.SetInt ("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
			// Turn off backface culling, depth writes, depth test.
			mat.SetInt ("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
			mat.SetInt ("_ZWrite", 0);
			mat.SetInt ("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
		}

		if(front.Length > 0){
			GL.PushMatrix();
			mat.SetPass(0);
			GL.Begin(GL.LINES);
			GL.Color(Color.red);
			GL.Vertex(front[0]);
			GL.Vertex(front[1]);
			GL.Vertex(front[1]);
			GL.Vertex(front[2]);
			GL.Vertex(front[2]);
			GL.Vertex(front[3]);
			GL.Vertex(front[3]);
			GL.Vertex(front[0]);

			GL.Vertex(back[0]);
			GL.Vertex(back[1]);
			GL.Vertex(back[1]);
			GL.Vertex(back[2]);
			GL.Vertex(back[2]);
			GL.Vertex(back[3]);
			GL.Vertex(back[3]);
			GL.Vertex(back[0]);

			
			GL.Vertex(front[0]);
			GL.Vertex(back[0]);

			GL.Vertex(front[1]);
			GL.Vertex(back[1]);

			GL.Vertex(front[2]);
			GL.Vertex(back[2]);
			
			GL.Vertex(front[3]);
			GL.Vertex(back[3]);

	//		Test for Speed hit with lots of lines
	//		for(int x = 0; x < 100; x++){
	//			for(int y = 0; y < 1000; y++){
	//				GL.Vertex(new Vector2(x * 2, y));
	//				GL.Vertex(new Vector2(x * 2+ 2, y));
	//			}
	//		}

			GL.End();
			GL.PopMatrix();
		}
	}

	public IEnumerator UploadPNG(string imageName) {
		// We should only read the screen buffer after rendering is complete
		yield return new WaitForEndOfFrame();
		
		// Create a texture the size of the screen, RGB24 format
		int width = Screen.width;
		int height = Screen.height;
		Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
		
		// Read screen contents into the texture
		tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
		tex.Apply();
		
		// Encode texture into PNG
		byte[] bytes = tex.EncodeToPNG();
		Destroy(tex);
		
		// For testing purposes, also write to a file in the project folder
		File.WriteAllBytes(Application.dataPath + "/../" + imageName + ".png", bytes);


		
		
		// Create a Web Form
		//		WWWForm form = new WWWForm();
		//		form.AddField("frameCount", Time.frameCount.ToString());
		//		form.AddBinaryData("fileUpload", bytes);
		//		
		//		// Upload to a cgi script
		//		WWW w = new WWW("http://localhost/cgi-bin/env.cgi?post", form);
		//		yield return w;
		//		if (w.error != null)
		//			print(w.error);
		//		else
		//			print("Finished Uploading Screenshot");
	}

}
