using UnityEngine;

public class PersistentObject : MonoBehaviour
{
    [SerializeField] private string uniqueID;

    private void Start()
    {
        if (GameController.persistentDestroyedObjects.Contains(uniqueID))
        {
            Destroy(gameObject);
        }
    }

    public void MarkAsDestroyed()
    {
        if (!string.IsNullOrEmpty(uniqueID))
        {
            GameController.persistentDestroyedObjects.Add(uniqueID);
        }
    }

    [ContextMenu("Generate ID")]
    public void GenerateID()
    {
        uniqueID = System.Guid.NewGuid().ToString();
    }
}
