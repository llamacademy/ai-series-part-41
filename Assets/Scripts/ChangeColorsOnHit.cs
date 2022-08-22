using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ChangeColorsOnHit : MonoBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        Color[] colors = mesh.colors;

        if (collision.collider.GetComponent<CharacterController>() != null)
        {
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = Color.green;
            }
        }
        else
        {
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = Color.red;
            }
        }
        mesh.colors = colors;
    }

    private void OnDisable()
    {
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        Color[] colors = mesh.colors;
        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = Color.white;
        }
        mesh.colors = colors;
    }
}
