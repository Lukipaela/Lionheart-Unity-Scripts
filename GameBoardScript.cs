using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameBoardScript : MonoBehaviour
{

    public static GameObject[,] gameBoardTileArray;

    private void Start()
    {
        //assign the game board tile default colors to the GameSettings storage variable 
        GameSettings.defaultBoardTileColor = transform.GetChild(0).GetComponent<MeshRenderer>().material.color; 
    }

    public void CreateGameBoardTiles()
    {
        /*NOTE 
         * this script is no longer in use, it was a one-time utility to generate and relate the board tiles. 
        *it is being preserved in case it is needed again, or for reference logic as needed 
        */
        gameBoardTileArray = new GameObject[,] { { null, null, null, null, null, null, null, null, null }
            ,{ null, null, null, null, null, null, null, null, null }
            ,{ null, null, null, null, null, null, null, null, null }
            ,{ null, null, null, null, null, null, null, null, null }
            ,{ null, null, null, null, null, null, null, null, null }
            ,{ null, null, null, null, null, null, null, null, null }
            ,{ null, null, null, null, null, null, null, null, null }
            ,{ null, null, null, null, null, null, null, null, null }
            ,{ null, null, null, null, null, null, null, null, null }
        };
        //this method has the task of creating the game board when the game leaves the main menu. 
        //get the prefab address for the tiles we want to spawn
        string tilePrefabAddress = "Prefabs/Structures/BoardTile";
        float tileDimension = 1;
        //get game board location and center
        //translate to the center location for the next tile to be created
        Vector3 nextTileLocation = transform.position + new Vector3(-4 * tileDimension, 0.5f, -4 * tileDimension);

        //create the 81 tiles that make up the game board, store in 2d array
        for (int i = 0; i < 9; i++)
        {
            for (int j = 0; j < 9; j++)
            {
                GameObject newTile = Instantiate(Resources.Load<GameObject>(tilePrefabAddress), nextTileLocation, transform.rotation);
                BoardTileScript tileScript = newTile.GetComponent<BoardTileScript>();
                gameBoardTileArray[i,j] = newTile;
                tileScript.row = i;
                tileScript.column = j;
                //move the next tile location over one space
                nextTileLocation = nextTileLocation + new Vector3(tileDimension, 0, 0);
            }
            //return the next tile column to column 0, but move to next row
            nextTileLocation = nextTileLocation + new Vector3(-9 * tileDimension, 0, tileDimension);
        }//tile creation loop

        //cycle through the now populated array and build associations 
        //get X/Z translations to work toward the location for the first tile
        for (int i = 0; i < 9; i++)
        {
            for (int j = 0; j < 9; j++)
            {
                BoardTileScript thisTileScript = gameBoardTileArray[i, j].GetComponent<BoardTileScript>();
                if (i > 0)
                    thisTileScript.adjacentTileLeft = gameBoardTileArray[i - 1, j];
                if (i < 8)
                    thisTileScript.adjacentTileRight = gameBoardTileArray[i + 1, j];
                if (j > 0)
                    thisTileScript.adjacentTileTop = gameBoardTileArray[i, j-1];
                if (j < 8)
                    thisTileScript.adjacentTileBottom = gameBoardTileArray[i, j+1];
            }
            //return the next tile column to column 0, but move to next row
            nextTileLocation = nextTileLocation + new Vector3(-8 * tileDimension, 0, tileDimension);
        }//tile association loop


    }//CreateGameBoardTiles

}//class
