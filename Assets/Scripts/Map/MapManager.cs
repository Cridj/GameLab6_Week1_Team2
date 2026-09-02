using UnityEngine;

enum Dir
{
    Up,Down,Right,Left
}

public class MapManager : MonoBehaviour
{
    public PlaneInfo planeInfo;

    public PlaneInfo[,] planes = new PlaneInfo[3, 3];

    public float planeSize = 100f;

    public Transform player;
    public Vector2Int prevPlayerChunkPos;
    public Vector2Int currentPlayerChunkPos;

    private void Start()
    {
        // Initialize the planes array
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                planes[i, j] = Instantiate(planeInfo, transform);
                planes[i, j].chunk = new Vector2Int(i, j);
                planes[i, j].index = new Vector2Int(i, j);
                planes[i, j].transform.position = new Vector3(planes[i, j].chunk.x * planeSize + planeSize / 2, 0, planes[i, j].chunk.y * planeSize + planeSize / 2);
            }
        }
    }

    private void Update()
    {
        CheckPlayerChunkChanged();
    }

    public void GetPlayerChunkPos()
    {
        int chunkX = Mathf.FloorToInt(player.position.x / planeSize);
        int chunkY = Mathf.FloorToInt(player.position.z / planeSize);
        currentPlayerChunkPos = new Vector2Int(chunkX, chunkY);
    }

    public void UpdatePlane(Vector2Int chunk, Vector2Int index)
    {
        index.x = Mod(index.x, 3);
        index.y = Mod(index.y, 3);
        //청크에 맞는 좌표로 이동
        planes[index.x, index.y].chunk = chunk;
        planes[index.x, index.y].transform.position = new Vector3(chunk.x * planeSize + planeSize / 2, 0, chunk.y * planeSize + planeSize / 2);
    }

    private int Mod(int value, int size)
    {
        return((value % size) + size) % size;
    }

    public bool CheckPlayerChunkChanged()
    {
        GetPlayerChunkPos();
        if (currentPlayerChunkPos != prevPlayerChunkPos)
        {

            if(prevPlayerChunkPos.x == currentPlayerChunkPos.x)
            {
                if(prevPlayerChunkPos.y < currentPlayerChunkPos.y)
                {
                    int leftXChunk = currentPlayerChunkPos.x - 1;
                    int rightXChunk = currentPlayerChunkPos.x + 1;

                    UpdatePlane(new Vector2Int(leftXChunk, currentPlayerChunkPos.y + 1), new Vector2Int(leftXChunk % 3, (currentPlayerChunkPos.y + 1) % 3));
                    UpdatePlane(new Vector2Int(rightXChunk, currentPlayerChunkPos.y + 1), new Vector2Int(rightXChunk % 3, (currentPlayerChunkPos.y + 1) % 3));
                    UpdatePlane(new Vector2Int(currentPlayerChunkPos.x, currentPlayerChunkPos.y + 1), new Vector2Int(currentPlayerChunkPos.x % 3, (currentPlayerChunkPos.y + 1) % 3));
                }
                else
                {
                    int leftXChunk = currentPlayerChunkPos.x - 1;
                    int rightXChunk = currentPlayerChunkPos.x + 1;

                    UpdatePlane(new Vector2Int(leftXChunk, currentPlayerChunkPos.y - 1), new Vector2Int(leftXChunk % 3, (currentPlayerChunkPos.y - 1) % 3));
                    UpdatePlane(new Vector2Int(rightXChunk, currentPlayerChunkPos.y - 1), new Vector2Int(rightXChunk % 3, (currentPlayerChunkPos.y - 1) % 3));
                    UpdatePlane(new Vector2Int(currentPlayerChunkPos.x, currentPlayerChunkPos.y - 1), new Vector2Int(currentPlayerChunkPos.x % 3, (currentPlayerChunkPos.y - 1) % 3));
                }
            }
            else if(prevPlayerChunkPos.y == currentPlayerChunkPos.y)
            {
                if (prevPlayerChunkPos.x < currentPlayerChunkPos.x)
                {
                    int leftYChunk = currentPlayerChunkPos.y - 1;
                    int rightYChunk = currentPlayerChunkPos.y + 1;

                    UpdatePlane(new Vector2Int(currentPlayerChunkPos.x + 1, leftYChunk), new Vector2Int((currentPlayerChunkPos.x + 1) % 3, leftYChunk % 3));
                    UpdatePlane(new Vector2Int(currentPlayerChunkPos.x + 1, rightYChunk), new Vector2Int((currentPlayerChunkPos.x + 1) % 3, rightYChunk % 3));
                    UpdatePlane(new Vector2Int(currentPlayerChunkPos.x + 1, currentPlayerChunkPos.y), new Vector2Int((currentPlayerChunkPos.x + 1) % 3, currentPlayerChunkPos.y % 3));
                }
                else
                {
                    int leftYChunk = currentPlayerChunkPos.y - 1;
                    int rightYChunk = currentPlayerChunkPos.y + 1;

                    UpdatePlane(new Vector2Int(currentPlayerChunkPos.x - 1, leftYChunk), new Vector2Int((currentPlayerChunkPos.x - 1) % 3, leftYChunk % 3));
                    UpdatePlane(new Vector2Int(currentPlayerChunkPos.x - 1, rightYChunk), new Vector2Int((currentPlayerChunkPos.x - 1) % 3, rightYChunk % 3));
                    UpdatePlane(new Vector2Int(currentPlayerChunkPos.x - 1, currentPlayerChunkPos.y), new Vector2Int((currentPlayerChunkPos.x - 1) % 3, currentPlayerChunkPos.y % 3));
                }
            }
            prevPlayerChunkPos = currentPlayerChunkPos;

            return true;
        }
        else
            return false;
    }

}
