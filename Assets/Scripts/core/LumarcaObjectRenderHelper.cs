using UnityEngine;
using System.Collections;

public class LumarcaObjectRenderHelper : MonoBehaviour {

	public Material material;
	public bool fastLessAccurate = false;

	public Vector3[] transformedVerts;
	public Vector3[] transformedNormals;
	public Mesh mesh;

	public bool drawDots = true;
	
	// Use this for initialization
	void Start () {

		MeshFilter mFilter = GetComponent<MeshFilter>();

		if(mFilter != null){
			mesh = mFilter.sharedMesh;
		} else {
			mesh = new Mesh();
			mesh = GetComponent<SkinnedMeshRenderer>().sharedMesh;
		}

		transformedVerts = new Vector3[mesh.vertices.Length];
		transformedNormals = new Vector3[mesh.normals.Length];

		for(int i = 0; i < transformedVerts.Length; i++){
			transformedVerts[i] = new Vector3();
			transformedNormals[i] = new Vector3();
		}


	}
	
	// Update is called once per frame
	void Update () {
		bool baked = false;

		MeshFilter mFilter = GetComponent<MeshFilter>();
		
		if(mFilter != null){
			mesh = mFilter.sharedMesh;
		} else {
			baked = true;
//			Debug.Log(mesh.vertices[0]);

			SkinnedMeshRenderer skin = this.GetComponent<SkinnedMeshRenderer> ();
			Mesh bakedMesh = new Mesh();
			skin.BakeMesh(bakedMesh);
			mesh = bakedMesh;
//			mesh = skin.sharedMesh;
//			Debug.Log("baked");
//			Debug.Log(mesh.vertices[0]);

		}

		Vector3[] verts = mesh.vertices;
		Vector3[] normals = mesh.normals;

		Vector3 position = transform.position;
		Quaternion rot = transform.rotation;

//		if(Time.frameCount == 2){
		for(int i = 0; i < transformedVerts.Length; i++){
			
//			transformedVerts[i].Set(verts[i].x * scale.x,
//			                       verts[i].y * scale.y,
//			                       verts[i].z * scale.z);
//
//			transformedVerts[i] = rot * transformedVerts[i];
//
//			transformedVerts[i] += position;

			
			transformedNormals[i] = rot * normals[i];
			if(!baked){
				transformedVerts[i] = transform.TransformPoint(verts[i]);
			} else {
				transformedVerts[i] = rot *  verts[i];
				transformedVerts[i] += position;
			}
		}
//		}
	}
}
