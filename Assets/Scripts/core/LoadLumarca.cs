using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using SimpleJSON;

public class LoadLumarca : MonoBehaviour {

	public bool calibration;
	public static int currentLine = 0;
	bool drawAll = false;

	public Vector3[] pos;
	List<Vector3> posList = new List<Vector3>();
	public Material mat;

	public bool drawCube = true;

	public Vector3[] front;
	public Vector3[] back;
	
	public GameObject[] gameObjects;
	public GameObject[] lineRenderers;
	
	Vector3 down = Vector3.down;
	
	Vector3 temp = new Vector3();
	
	Vector3 vec1 = new Vector3();
	Vector3 vec2 = new Vector3();
	Vector3 vec3 = new Vector3();

	int[] tris;
	Vector3[] verts;
	Vector3[] normals;

	List<float> result;

	List<Vector3> pool;

	List<int> depthList;

	public bool debugMessages = false;

	Ray ray = new Ray();

	// Use this for initialization
	void Start () {
		Cursor.visible = false;

		SetMaterial();
		pool = new List<Vector3>();

		for(int i = 0; i < 10000; i++){
			pool.Add(new Vector3());
		}

		result = new List<float>();

		TextAsset asset = Resources.Load("positions") as TextAsset;
		Stream s = new MemoryStream(asset.bytes);
		BinaryReader reader = new BinaryReader(s);

//		BinaryReader reader = new BinaryReader(File.Open("positions.json", FileMode.Open));
		JSONNode ja = SimpleJSON.JSONNode.Deserialize(reader);

		for(int i = 0; i < ja.Count; i++){
			Vector3 vec = UtilScript.JsonToVector3(ja[i]);

			posList.Add(vec);
		}

		pos = posList.ToArray();

		Debug.Log("Num Lines: " + pos.Length);

		CameraFrustumScript cfs = Camera.main.GetComponent<CameraFrustumScript>();
		
		front = cfs.GetFrontPlane();
		back = new Vector3[4];
		
		for(int i = 0; i < front.Length; i++){
			back[i] = UtilScript.CloneVec3(front[i]);
			back[i].z = Camera.main.farClipPlane - Camera.main.nearClipPlane;
		}
	}

	void Update(){	

		if(Input.GetKeyDown(KeyCode.C)){
			calibration = !calibration;
		}

		if(calibration){
			if(Input.GetKeyDown(KeyCode.UpArrow)){
				currentLine++;
			} else if(Input.GetKeyDown(KeyCode.DownArrow)){
				currentLine--;
			}

			if(Input.GetKey(KeyCode.W)){
				currentLine++;
			} else if(Input.GetKey(KeyCode.S)){
				currentLine--;
			}

			if(currentLine < 0){
				currentLine = pos.Length - 1;
			} else if(currentLine >= pos.Length){
				currentLine = 0;
			}

			if(Input.GetKey(KeyCode.Space)){
				drawAll = true;
			} else {
				drawAll = false;
			}
		}
	}
	
	void OnPostRender() {
		if(calibration){
			showCalibrationLines();
		} else
			drawScene();
	}
	
	void OnDrawGizmos(){
		if(calibration){
			showCalibrationLines();
		} else
			drawScene();
	}

	void showCalibrationLines(){
		if(drawCube)
			DrawBox();

		if(depthList == null){
			makeDepthList();
		}

		Vector3 line = pos[depthList[currentLine]];

		Vector3 top = UtilScript.CloneVec3(line);
		Vector3 bottom = UtilScript.CloneVec3(line);
		
		top.y = front[0].y;
		bottom.y = front[2].y;
		
		DrawLine(top, bottom, mat);

		if(drawAll){
			foreach(Vector3 vec in pos){
				top = UtilScript.CloneVec3(vec);
				bottom = UtilScript.CloneVec3(vec);
				
				top.y = front[0].y;
				bottom.y = front[2].y;
				
				DrawLine(top, bottom, mat);
			}
		}
	}
	
	void drawScene(){
		
		GL.PushMatrix();
		
		mat.SetPass(0);
		
		if(drawCube)
			DrawBox();
		
		foreach(GameObject go in gameObjects){
			drawMesh(go);
		}

		foreach(GameObject go in lineRenderers){
			LumarcaLineRenderer[] llr = go.GetComponentsInChildren<LumarcaLineRenderer>();
			foreach(LumarcaLineRenderer lineRender in llr){
				if(lineRender.enabled){
					for(int lineNum = 0; lineNum < pos.Length; lineNum++){
						Vector3 vec =pos[lineNum];
						Vector3[] points = lineRender.GenerateLine(lineNum, vec, 
						                                           front[1].x, front[0].x,
						                                           front[2].y, front[0].y,
						                                           back[0].z,  front[0].z);
						for(int i = 0; i+1 < points.Length; i+=2){
							if(lineRender.drawDots){
								DrawLine(points[i], points[i+1], lineRender.mat);
							} else {
								DrawLineWithoutDots(points[i], points[i+1], lineRender.mat);
							}
						}
					}
				}
			}
		}

		GL.PopMatrix();
	}


	void drawMesh(GameObject rObject){
		LumarcaObjectRenderHelper[] lumarcaMeshes = rObject.GetComponentsInChildren<LumarcaObjectRenderHelper>();

		Vector3 pt1 = new Vector3();
		Vector3 pt2 = new Vector3();

		int checkNum = 0;
				
		for(int g = 0; g < lumarcaMeshes.Length; g++){
			
			LumarcaObjectRenderHelper lmh = lumarcaMeshes[g];
//			Mesh mesh = filter.sharedMesh;

			GameObject gObject = lmh.gameObject;

			BoxCollider boxCollider = lmh.GetComponent<BoxCollider>();

			verts = lmh.GetComponent<LumarcaObjectRenderHelper>().transformedVerts;
			normals = lmh.GetComponent<LumarcaObjectRenderHelper>().transformedNormals;
			
			bool fast = lmh.GetComponent<LumarcaObjectRenderHelper>().fastLessAccurate;
			
			tris = lmh.mesh.triangles;

			Material rMat = mat;
			
			if(lmh != null){
				rMat = lmh.material;
			}

			for(int c = 0; c < pos.Length; c++){

				Vector3 vec = pos[c];

				vec.y = front[2].y * 10;
				
				pt1.x = vec.x;
				pt1.z = vec.z;
				pt2.x = vec.x;
				pt2.z = vec.z;

				if(PositionInBox(boxCollider, gObject.transform.lossyScale, gObject.transform.position, vec)){

					List<float> intersections;
					if(!fast){
						intersections = GetIntersectList(vec);
					} else {
						intersections = GetIntersectListFast(vec);
					}

					checkNum++;

					intersections.Sort();

					for(int i = 0; i < intersections.Count; i+=2){

						if(i+1 >= intersections.Count){
							if(debugMessages){
								Debug.Log("UNEVEN!!! " + intersections.Count);
							}
							break;
						} else {
							pt1.y = intersections[i];
							pt2.y = intersections[i + 1];
						}
						
						if(pt1.y > front[2].y){
							pt1.y = front[2].y;
						} 

						if(pt2.y > front[2].y){
							pt2.y = front[2].y;
						} 
						
						if(pt1.y < front[0].y){
							pt1.y = front[0].y;
						} 
						
						if(pt2.y < front[0].y){
							pt2.y = front[0].y;
						} 

						if(pt1.y != pt2.y){
							if(lmh.drawDots){
								DrawLine(pt1, pt2, rMat);
							} else {
								DrawLineWithoutDots(pt1, pt2, rMat);
							}
						}
					}
				}
			}
		}
		
//		Debug.Log("checkNum: " + checkNum * mesh.vertexCount/3);
//		Debug.Log("normals: " + mesh.vertices.Length);
//		Debug.Log("tris: " + mesh.normals.Length);
	}

	bool PositionInBox2(BoxCollider box, Vector3 scale, Vector3 center, Vector3 pos){
		ray.direction.Set(0, -1, 0);
		ray.origin.Set (pos.x, pos.y, pos.z);

//		Debug.Log(pos.y * 10);
		
		return box.bounds.IntersectRay(ray);


	}

	bool PositionInBox(BoxCollider box, Vector3 scale, Vector3 center, Vector3 pos){
		Vector3 size = box.size;
		
//		Debug.Log("Box Front: " + (center.z - size.z/2 * scale.z));
//		Debug.Log("Box Back : " + (center.z + size.z/2 * scale.z));

//		return true;
		return (center.x - size.x * scale.x * 1.1f <= pos.x && 
		        center.x + size.x * scale.x * 1.1f >= pos.x &&
		        center.z - size.z * scale.z * 1.1f <= pos.z && 
		        center.z + size.z * scale.z * 1.1f >= pos.z);
	}

	void SetMaterial(){
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
	}

	void DrawBox(){

		if(front.Length > 0){
			DrawLine(front[0], front[1]);
			DrawLine(front[1], front[2]);
			DrawLine(front[2], front[3]);
			DrawLine(front[0], front[3]);

			DrawLine(back[0], back[1]);
			DrawLine(back[1], back[2]);
			DrawLine(back[2], back[3]);
			DrawLine(back[0], back[3]);
			
			DrawLine(back[0], front[0]);
			DrawLine(back[1], front[1]);
			DrawLine(back[2], front[2]);
			DrawLine(back[3], front[3]);
		}
	}
	void DrawLine(Vector3 top, Vector3 bottom){
		DrawLine(top, bottom, mat);
	}

	void DrawLine(Vector3 top, Vector3 bottom, Material rMat){

		rMat.SetPass(0);
		
		GL.Begin(GL.LINES);

		if(calibration){
			GL.Color(Color.red);
		}
		GL.Vertex(top);
		GL.Vertex(bottom);
		
		GL.End();

		mat.SetPass(0);
		
		Vector3 top2 = UtilScript.CloneVec3(top);
		top2.y += 10;
		Vector3 bottom2 = UtilScript.CloneVec3(bottom);
		bottom2.y -= 10;
		
		GL.Begin(GL.LINES);
		GL.Color(Color.white);
		
		GL.Vertex(top);
		GL.Vertex(top2);
		
		GL.End();
		
		GL.Begin(GL.LINES);
		GL.Color(Color.white);
		
		GL.Vertex(bottom);
		GL.Vertex(bottom2);
		
		GL.End();
	}
	
	void DrawLineWithoutDots(Vector3 top, Vector3 bottom){
		DrawLine(top, bottom, mat);
	}

	void DrawLineWithoutDots(Vector3 top, Vector3 bottom, Material mat){

		mat.SetPass(0);

		GL.Begin(GL.LINES);

		GL.Vertex(top);
		GL.Vertex(bottom);
		
		GL.End();
	}

	//TODO calc vert positions once, save performance

	List<float> GetIntersectList(Vector3 vec){
		result.Clear();

		for(int i = 0; i < tris.Length; i+=3){
			
			int vert1 = tris[i];
			int vert2 = tris[i + 1];
			int vert3 = tris[i + 2];
						
			vec1 = verts[vert1];
			vec2 = verts[vert2];
			vec3 = verts[vert3];
			
			Vector3 inter = UtilScript.CloneVec3(vec);
			inter.y = front[2].y * 10;

			Vector3 normal = (normals[vert1] + normals[vert2] + normals[vert3])/3;

			inter = checkIntersectTri(vec1, vec2, vec3, inter, down, temp, normal);
			
			if(inter.y > -1001){
				result.Add(inter.y);
			}
		}
		
		return result;
	}

	List<float> GetIntersectListFast(Vector3 vec){
		result.Clear();

		for(int i = 0; i < tris.Length; i+=3){
			
			int vert1 = tris[i];
			int vert2 = tris[i + 1];
			int vert3 = tris[i + 2];

			vec1 = verts[vert1];
			vec2 = verts[vert2];
			vec3 = verts[vert3];
			
			if(pointInTriangle(vec1.x, vec1.z, 
			                   vec2.x, vec2.z,
			                   vec3.x, vec3.z,
			                   vec.x, vec.z)){
				result.Add((vec1.y + vec2.y + vec3.y)/3f);
			}
		}

		return result;
	}

	bool pointInTriangle(float x1, float y1, float x2, float y2, float x3, float y3, float x, float y)
	{
		float denominator = ((y2 - y3)*(x1 - x3) + (x3 - x2)*(y1 - y3));
		float a = ((y2 - y3)*(x - x3) + (x3 - x2)*(y - y3)) / denominator;
		float b = ((y3 - y1)*(x - x3) + (x1 - x3)*(y - y3)) / denominator;
		float c = 1 - a - b;
		
		return 0 <= a && a <= 1 && 0 <= b && b <= 1 && 0 <= c && c <= 1;
	}
	
	Vector3 checkIntersectTri(Vector3 pt1, Vector3 pt2, Vector3 pt3,
	                          Vector3 linept, Vector3 vect, Vector3 result, Vector3 norm) {
	
		result.y = -10000;

//		norm = getNormal(pt1, pt2, pt3);

		if(norm.y < 0){

			Vector3 temp = pt2;
			pt2 = pt1;
			pt1 = temp;
			
			norm *= -1;//getNormal(pt1, pt2, pt3);
		} 
				
		// dot product of normal and line's vector if zero line is parallel to
			// triangle
		float dotprod = norm.x * vect.x + norm.y * vect.y + norm.z * vect.z;
//		dotprod = Vector3.Dot(norm, vect);
		
		if (dotprod < 0) {

			// Find point of intersect to triangle plane.
			// find t to intersect point
			float t =
				-(norm.x * (linept.x - pt1.x) + norm.y * (linept.y - pt1.y) + norm.z
			      * (linept.z - pt1.z))
				/ (norm.x * vect.x + norm.y * vect.y + norm.z * vect.z);
			
			// if ds is neg line started past triangle so can't hit triangle.
			if (t < 0){
				return result;
			}
			
			result.x = linept.x + vect.x * t;
			result.y = linept.y + vect.y * t;
			result.z = linept.z + vect.z * t;

			if (checkSameClockDir(pt1, pt2, result, norm) &&
			    checkSameClockDir(pt2, pt3, result, norm) &&
			    checkSameClockDir(pt3, pt1, result, norm)) {
						// answer in pt_int is inside triangle
						return result; // ANSWER
			}
		}

		result.Set(0, -10000, 0);
		return result;
	}

	private static Vector3 getNormal(Vector3 pt1, Vector3 pt2, Vector3 pt3){
		
		float V1x, V1y, V1z;
		float V2x, V2y, V2z;
		Vector3 norm = new Vector3();
		
		// vector form triangle pt1 to pt2
		V1x = pt2.x - pt1.x;
		V1y = pt2.y - pt1.y;
		V1z = pt2.z - pt1.z;
		
		// vector form triangle pt2 to pt3
		V2x = pt3.x - pt2.x;
		V2y = pt3.y - pt2.y;
		V2z = pt3.z - pt2.z;
		
		// vector normal of triangle
		norm.x = (V1y * V2z - V1z * V2y);
		norm.y = (V1z * V2x - V1x * V2z);
		norm.z = (V1x * V2y - V1y * V2x);
		
		return norm;
	}

	private  bool checkSameClockDir(Vector3 pt1, Vector3 pt2,
	                                      Vector3 pt3, Vector3 norm) {

		// normal of trinagle
		float testi = (((pt2.y - pt1.y) * (pt3.z - pt1.z)) - ((pt3.y - pt1.y) * (pt2.z - pt1.z)));
		float testj = (((pt2.z - pt1.z) * (pt3.x - pt1.x)) - ((pt3.z - pt1.z) * (pt2.x - pt1.x)));
		float testk = (((pt2.x - pt1.x) * (pt3.y - pt1.y)) - ((pt3.x - pt1.x) * (pt2.y - pt1.y)));
		
		// Dot product with triangle normal
		float dotprod = testi * norm.x + testj * norm.y + testk * norm.z;
		
		// answer
		if (dotprod < 0)
			return false;
		else
			return true;
	}

	private void makeDepthList(){
		depthList = new List<int>(); 
		depthList.Add(0);
		
		for(int i = 1; i < pos.Length; i++){
			
			Vector3 current = pos[i];
			bool inserted = false;
			
			for(int c = 0; c < i; c++){
				Vector3 prev = pos[depthList[c]];
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
}
