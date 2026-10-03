using AudioData;
using UnityEngine;

[System.Serializable]
public class HitboxData
{
    #region Estruturas
    /* - - - - -
     * Enumerator do formato do hitbox
     * - - - - - */
    public enum Shape
    {
        Box,
        Sphere
    }
    #endregion

    #region Variáveis
    // VARIÁVEIS VISÍVEIS
    [field: Header("Configurações")]
        [field: SerializeField, Tooltip("Formato do hitbox")]
    public Shape Type { get; private set; }
        [field: SerializeField, Tooltip("Posição da hitbox em relação ao personagem")]
    public Vector3 LocalPosition { get; private set; }
        [field: SerializeField, Tooltip("Rotação da hitbox em relação ao personagem")]
    public Vector3 LocalRotation { get; private set; }

    [field: Header("Box")]
        [field: SerializeField, Tooltip("Tamanho da hitbox quando o formato for Box")]
    public Vector3 Size { get; private set; } = Vector3.one;

    [field: Header("Sphere")]
        [field: SerializeField, Min(0) , Tooltip("Raio da hitbox quando o formato for Sphere")]
    public float Radius { get; private set; } = 0.5f;
    #endregion

    #region Main
    public Quaternion Rotation
    {
        get
        {
            return Quaternion.Euler(LocalRotation);
        }
    }

    #endregion
}