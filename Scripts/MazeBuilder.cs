using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

// Note: I don't need to do this especially with the way the tiles are but I can make it so when a tile is recorded, then it removes the walls that were recorded so that the next tile does not double up the walls.

// Next steps
//      Bring this into main scene
//      Make main scene
//      Give GameManager control of three mazes, each with a unique number and sizes.
//      Randomly choose a starting point as well. X
//      Press to open doors for maze access. Then they teleport you over.
// Future steps
//      Timer, slowly lose health
//      Randomly spawn in pits
//      Oh shit there was the idea to make certain areas inaccessible.
//          Not doing that because of random nature of maze.



public class MazeBuilder : MonoBehaviour
{
    // This class is for actually building the maze.
    public int MazeNumber;
    public int MazeSize;
    public float TileSize;
    public float Scale;

    private Vector3 ParentPosition;

    private string MazeName;
    private MazeGenerator MazeGenerator;
    private Vector3 StartingPoint;
    private Vector3 EndingPoint;

    // Where the CoordsToBinary object returns a string which is a 4bit binary, translate that to decimal and use that as an index to search this array for the GameObject tile of which their are exactly 16.
    private GameObject[] TileGameObjects;
    // The corresponding rotation in degrees is stored in the same way.
    private float[] TileRotations;

    // All prefabs to spawn in to maze.
    public GameObject Full;
    public GameObject Corner;
    public GameObject Split;
    public GameObject Forward;
    public GameObject Empty;
    public GameObject CulDeSac;
    public GameObject End;

    void Awake()
    {
        MazeGenerator = new MazeGenerator(MazeSize);
        MazeName = "Maze #" + MazeNumber;

        ParentPosition = GetComponentInParent<Transform>().position;

        // Assign the tiles objects to the array.
        // Also assign the rotations.
        TileGameObjects = new GameObject[16];
        TileRotations = new float[16];

        // DO NOT EDIT THESE NUMBERS.
        // There are sixteen possible tiles and orientations for them.
        // Translate walla to binary looks at the cells of the maze and translate them to 4bit binary number that then get coverted to decimal. That's a bit complex for this lol.
        TileGameObjects[0] = Empty;
        TileRotations[0] = 0;
        TileGameObjects[1] = Split;
        TileRotations[1] = 270;
        TileGameObjects[2] = Split;
        TileRotations[2] = 180;
        TileGameObjects[3] = Corner;
        TileRotations[3] = 180;
        TileGameObjects[4] = Split;
        TileRotations[4] = 90;
        TileGameObjects[5] = Forward;
        TileRotations[5] = 0;
        TileGameObjects[6] = Corner;
        TileRotations[6] = 90;
        TileGameObjects[7] = CulDeSac;
        TileRotations[7] = 180;
        TileGameObjects[8] = Split;
        TileRotations[8] = 0;
        TileGameObjects[9] = Corner;
        TileRotations[9] = 270;
        TileGameObjects[10] = Forward;
        TileRotations[10] = 90;
        TileGameObjects[11] = CulDeSac;
        TileRotations[11] = 270;
        TileGameObjects[12] = Corner;
        TileRotations[12] = 0;
        TileGameObjects[13] = CulDeSac;
        TileRotations[13] = 0;
        TileGameObjects[14] = CulDeSac;
        TileRotations[14] = 90;
        TileGameObjects[15] = Full;
        TileRotations[15] = 0;

        // BuildMaze();
    }

    // Update is called once per frame
    void Update()
    {

    }


    /**
     * Guess what this function does.
     * It generates a maze 
     * 
     */
    public void BuildMaze()
    {
        // These are the wall specifications for the generated maze.
        //      See MazeGenerator for how these specs represent the maze.
        int[][][] MazeWallSpecs = MazeGenerator.GetMaze();

        // Next step is to translate these specs to something representing a maze tile.
        //      There are six different tiles which may have different orientations.
        //      Corner, split, forward, end, full, and empty.
        // 
        // Corner has four orientations.
        // Split has four.
        // Forward has two.
        // End has four.
        // Full has one.
        // Empty has one.

        // This is the maze. Each coordinate in the maze has a 4 bit binary as a string representing one of 16 tile types of the maze.
        MazeDictionary MazeAsCoords = TranslateWallsToBinary(MazeWallSpecs);

        SpawnInMaze(MazeAsCoords);
    }

    // Returns a dictionary where the Tuple<int,int> key is the coordinates in the maze and the string is a four digit binary code representing the tile type and orientation.
    //      I.e. Dictionary<(x,y),the walls at this cell>
    private MazeDictionary TranslateWallsToBinary(int[][][] mazeWallSpecs)
    {
        MazeDictionary CoordsToBinary = new MazeDictionary();

        // Go through each index in the maze and figure out which walls it has.
        for (int row_index = 0; row_index < MazeSize; row_index++)
        {
            for (int col_index = 0; col_index < MazeSize; col_index++)
            {

                // Given a cell in the maze with all its walls.
                // So let's say the binary is in the following order:
                //   -        4
                //  | |      1 3        4321
                //   -        2
                // This would return: 1111
                // If we removed the right wall it would return: 1011
                // If we remove the top and bottom walls it would return: 0101

                string BinaryToAdd = "";

                // 1) Top wall
                BinaryToAdd += mazeWallSpecs[1][col_index][row_index];
                // 2) Right Wall
                BinaryToAdd += mazeWallSpecs[0][row_index][col_index + 1];
                // 3) Bottom Wall
                BinaryToAdd += mazeWallSpecs[1][col_index][row_index + 1];
                // 4) Left Wall
                BinaryToAdd += mazeWallSpecs[0][row_index][col_index];

                CoordsToBinary.Add(new Tuple<int,int> (row_index, col_index), BinaryToAdd);
            }
        }
        return CoordsToBinary;
    }


    private void SpawnInMaze(Dictionary<Tuple<int,int>, string> mazeAsCoords)
    {
        ArrayList CulDuSacs = new ArrayList();
        ArrayList PossibleStartingPoints = new ArrayList();

        // For each cell in the maze
        for (int row_index = 0; row_index < MazeSize; row_index++)
        {
            for (int col_index = 0; col_index < MazeSize; col_index++)
            {

                // Spawn in a tile corresponding to that cell's wall setup.
                string binary_ = mazeAsCoords[new Tuple<int,int> (row_index,col_index)];
                int binAsDecimal = StrBinaryToDecimal(binary_);

                GameObject CurrentTile = TileGameObjects[binAsDecimal];

                GameObject tile = Instantiate
                (
                    CurrentTile, 
                    ((new Vector3(  (row_index ) * TileSize,   0, (col_index) * TileSize)) + ParentPosition) * Scale, // (Coordinate * TileSize) + local position all times Scale
                    Quaternion.identity
                );
                tile.transform.localScale = new Vector3(Scale,Scale,Scale);

                tile.name = "Maze Tile @ " + row_index+":"+col_index;
                tile.transform.parent = GameObject.Find("Maze " + MazeNumber).transform;
                tile.transform.Rotate(0, TileRotations[binAsDecimal], 0);
                
                // If this tile is a Cul De Sac, then add it to the array so it may be a candidate for the end of the maze.
                if (binary_ == "0111" || binary_ == "1011" || binary_ == "1101" || binary_ == "1110")
                {
                    // Give this array both the GameObject and it's angle.
                    CulDuSacs.Add( new Tuple<GameObject,int> (tile, StrBinaryToDecimal(binary_)));
                }
                // Add all tiles to the possible starting points, to pick NEARLY randomly after.
                PossibleStartingPoints.Add(tile);

            }
        } 


        // Finally, this bit does TWO things.
        // 1) It replaces a random cul de sac tile with an end of maze tile.
        // 2) Grabs a random coordinate for the spawn point, makes sure it is of sufficient distance from the end.

        // Randomly select a cul de sac, get its position, delete it, and then replace it with an end tile.
        System.Random choose = new System.Random(); 
        // Get the index at random.
        int ToReplace = choose.Next(CulDuSacs.Count);
        // Grab the Tile, angle Tuple from the array.
        Tuple<GameObject, int> GOandAngle = (Tuple<GameObject,int>) CulDuSacs[ToReplace];
        // Get the position of the tile.
        Vector3 EndPosition = GOandAngle.Item1.transform.position;
        // Destroy the tile.
        GameObject TileToDestroy = (GameObject) GOandAngle.Item1; 
        GameObject.Destroy( TileToDestroy );
        // Replace it with a tile for the end of the maze.
        GameObject EndTile = Instantiate
        (
            End,
            EndPosition,
            Quaternion.identity
        );
        EndTile.transform.localScale = new Vector3(Scale,Scale,Scale);
        EndTile.transform.Rotate(0, TileRotations[GOandAngle.Item2],0);
        EndTile.transform.parent = GameObject.Find("Maze " + MazeNumber).transform;
        EndingPoint = EndTile.transform.position;

        // Choose a starting point at random.
        // Buuut make sure it is of sufficient distance away from the end.
        //      As the crow flies.
        for (int count = 0; count < MazeSize*MazeSize || PossibleStartingPoints.Count <= 0; count++)
        {
            int check = choose.Next(PossibleStartingPoints.Count);
            Vector3 possibleStartingPoint = ((GameObject) PossibleStartingPoints[check]).transform.position;

            // Calc distance between possible start and end.
            float distance = 
                Mathf.Sqrt
                ( 
                   Mathf.Pow( EndTile.transform.position.x - possibleStartingPoint.x, 2) 
                   + 
                   Mathf.Pow( EndTile.transform.position.z - possibleStartingPoint.z, 2) 
                );

            // If this possible starting point is more than or equal to half the length of the maze...
            if (distance >= ((float)MazeSize)/2f)
            {
                // ... then choose it as the starting point.
                StartingPoint = possibleStartingPoint + new Vector3(0,0.2f,0);
                break;
            }
        }
        if (StartingPoint == null)
        {
            StartingPoint = Vector3.zero;
            Debug.Log("There was a problem setting the starting point for the " + MazeName);
        }
    }

    private int StrBinaryToDecimal(string bin)
    {
        if (bin.Length != 4)
        {
            throw new Exception("A binary string was taken into this function but is not 4bit (of length four).");
        }

        int decimalReturn = 0;

        for (int unit = 0; unit<bin.Length; unit++)
        {
            if (bin[unit] == '1') decimalReturn += (int) Mathf.Pow(2,(bin.Length - 1) - unit);
        }

        return decimalReturn;
    }

    public Vector3 GetStartOfMaze()
    {
        if (StartingPoint == null) throw new Exception("Access to the starting point of Maze #" + MazeNumber + " was attempted but the maze hasn't been generated yet.");
        return StartingPoint + new Vector3(0,0.2f,0);
    }
    public Vector3 GetEndOfMaze()
    {
        return EndingPoint;
    }

    public void ResetMaze()
    {
        // Delete every tile. Tiles are stored in this parent.
        GameObject parent = GameObject.Find("Maze " + MazeNumber);
        while (parent.transform.childCount > 0)
        {
            DestroyImmediate(parent.transform.GetChild(0).gameObject);
        }
        // Build the new maze.
        BuildMaze();
    }

    private class MazeDictionary : Dictionary<Tuple<int, int>, string>
    {
        public MazeDictionary() : base() { }

        public override int GetHashCode()
        {
            // Start with a prime number.
            int hash = 17;

            // Go through each Value checking if they are strings.
            foreach (var value in this.Values)
            {
                if (value is string wallBinary)
                {
                    // Add hashcode of string multiplied by the prime 31 to the hash.
                    hash = hash * 31 + wallBinary.GetHashCode();
                }
            }
            return hash;
        }

        // The comparison between two mazes relies on two things.
        // 1) The size. If they have the same Count then they are the same size maze.
        // 2) The values. The value at each Key represents the walls of the maze. So, if all the cells in the maze have the same walls, then the mazes are the same.
        public override bool Equals(object obj)
        {
            if (obj is not MazeDictionary other || this.Count != other.Count)
                return false;

            MazeDictionary.ValueCollection otherVals = other.Values;
            MazeDictionary.ValueCollection thisVals = this.Values;

            // Compares every value IN ORDER. That's critical for this.
            for (int i = 0; i < this.Count; i++)
            {
                if (otherVals.ElementAt(i) != thisVals.ElementAt(i)) return false;
            }
            return true;
        }
    }

}
