using UnityEngine;
using System.Collections;

/// <summary>
/// ALL Configurations for a Block Type
/// </summary>
public class DecorationCollection : MonoBehaviour
{
    /// <summary>
    /// Specific Door Storage Slot
    /// </summary>
    public DecorationStorage DoorStorage;

    /// <summary>
    /// A clean no decorated Piece for this block type
    /// </summary>
    public DecorationPiece nonDecorationPiece;

    /// <summary>
    /// The type of collection of block
    /// </summary>
    public BlockType BlockTypeCollection;

    /// <summary>
    /// Each unique configured storage for this a block type
    /// </summary>
    public DecorationStorage[] Storages;

}
