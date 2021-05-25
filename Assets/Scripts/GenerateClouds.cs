using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public class CloudData
{

    public Vector3 pos;
    public Vector3 scale;
    public Quaternion rot;
    private bool _isActive;

    public bool isActive
    {
        get
        {
            return _isActive;
        }
    }
    public int x;
    public int y;
    public float distFromCam;

    public Matrix4x4 matrix
    {
        get
        {
            return Matrix4x4.TRS(pos, rot, scale);
        }
    }

    public CloudData(Vector3 pos, Vector3 scale,Quaternion rot, int x,int y, float distFromCam)
    {
        this.pos = pos;
        this.scale = scale;
        this.rot = rot;
        SetActive(true);
        this.x = x;
        this.y = y;
        this.distFromCam = distFromCam;
    }

    public void SetActive(bool desState)
    {
        _isActive = desState;
    }

} 
public class GenerateClouds : MonoBehaviour
{
    //meshes

    public Mesh cloudMesh;
    public Material cloudMat;


    //cloud data 
    public float cloudSize = 5;
    public float maxScale = 1;


    //noise gen
    public float timeScale = 1;
    public float texScale = 1;


    //cloud scaling information
    public float minNoiseSize = 0.5f;
    public float sizeScale = 0.25f;


    //culling  data
    public Camera cam;
    public int maxDist;


    // cloud groups
    public int batchesToCreate;

    private Vector3 prevCamPos;
    private float offsetX = 1;
    private float offsetY = 1;
    private List<List<CloudData>> batches = new List<List<CloudData>>();

    private List<List<CloudData>> BatchesToUpdate = new List<List<CloudData>>();

    private void Start()
    {
        for(int batchesX = 0; batchesX < batchesToCreate; batchesX++)
        {
            for (int batchesY = 0; batchesY< batchesToCreate; batchesY++)
            {
                BuildCloudBatch(batchesX, batchesY);
            }
        }
    }

    private void BuildCloudBatch(int xLoop,int yLoop)
    {
        bool markbatch = false;
        List<CloudData> currbatch = new List<CloudData>();

        for(int x = 0; x < 31; x++)
        {
            for (int y = 0; y< 31; y++)
            {
                addCloud(currbatch, x + xLoop * 31, y + yLoop * 31);
            }
        }
        markbatch = CheckForActiveBatch(currbatch);
        batches.Add(currbatch);



        if (markbatch) BatchesToUpdate.Add(currbatch);
    }
    private bool CheckForActiveBatch(List<CloudData> batch)
    {
        foreach(var cloud in batch)
        {
            cloud.distFromCam = Vector3.Distance(cloud.pos, cam.transform.position);
            if (cloud.distFromCam < maxDist) return true;
        }
        return false;
    }
    private void addCloud(List<CloudData> currbatch,int x, int y)
    {
        Vector3 position = new Vector3(transform.position.x + x * cloudSize, transform.position.y, transform.position.z + y * cloudSize);
        float disTocCam = Vector3.Distance(new Vector3(x, transform.position.y, y), cam.transform.position);
        currbatch.Add(new CloudData(position, Vector3.zero, Quaternion.identity, x, y, disTocCam));
    }
    private void Update()
    {
        makeNoise();
        offsetX += Time.deltaTime * timeScale;
        offsetY += Time.deltaTime * timeScale;
    }
    void makeNoise()
    {
        if(cam.transform.position == prevCamPos)
        {
            Updatebatches();
        }
        else
        {
            prevCamPos = cam.transform.position;
            UpdatebatchList();
            Updatebatches();
        }
        renderbatches();
        prevCamPos = cam.transform.position;
    }

  

    private void Updatebatches()
    {
        foreach(var batch in BatchesToUpdate)
        {
            foreach(var cloud in batch)
            {
                float size = Mathf.PerlinNoise(cloud.x * texScale + offsetX,cloud.y*texScale+offsetY);
                if (size > minNoiseSize)
                {
                    float localScaleX = cloud.scale.x;
                    if (!cloud.isActive)
                    {
                        cloud.SetActive(true);
                        cloud.scale = Vector3.zero;
                    }
                    if (localScaleX < maxScale)
                    {
                        ScaleCloud(cloud, 1);
                        if (cloud.scale.x > maxScale)
                        {
                            cloud.scale = new Vector3(maxScale, maxScale, maxScale);
                        }
                    }

                }
                else if (size < minNoiseSize)
                {
                    float localScale = cloud.scale.x;
                    ScaleCloud(cloud, -1);
                    if (localScale <= 0.1)
                    {
                        cloud.SetActive(false);
                        cloud.scale = Vector3.zero;
                    }

                }
            }
           
        }

    }

    private void ScaleCloud(CloudData cloud, int direction)
    {
        var x = sizeScale * Time.deltaTime * direction;
        var y = sizeScale * Time.deltaTime * direction;
        var z = sizeScale * Time.deltaTime * direction;
        cloud.scale += new Vector3(x, y, z);
    }
    private void UpdatebatchList()
    {
        BatchesToUpdate.Clear();
        foreach(var batch in batches)
        {
            if (CheckForActiveBatch(batch))
            {
                BatchesToUpdate.Add(batch);
            }
        }
    }
    private void renderbatches()
    {
        foreach(var batch in BatchesToUpdate){
            Graphics.DrawMeshInstanced(cloudMesh, 0, cloudMat, batch.Select((a) => a.matrix).ToList());

        }
    }

}
