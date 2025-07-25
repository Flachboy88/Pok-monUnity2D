using UnityEngine;
using UnityEngine.Tilemaps;

public class TriggerAnimatedTile : MonoBehaviour
{
    public Tilemap tilemap;
    public AnimatedTile animatedTileAsset;
    public Vector3Int tilePosition;

    public void TriggerAnimation()
    {
        Debug.Log("Triggering animation for tile at position: " + tilePosition);
        animatedTileAsset.triggerPlay = true;
        tilemap.RefreshTile(tilePosition);
        Invoke(nameof(ResetTrigger), animatedTileAsset.m_AnimatedSprites.Length / animatedTileAsset.m_MinSpeed);
    }

    void ResetTrigger()
    {
        animatedTileAsset.triggerPlay = false;
        tilemap.RefreshTile(tilePosition);
    }
}
