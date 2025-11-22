using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MazeGenerator
{
    // Visited parts of the maze search. 0 is not visited and 1 is visited.
    int[][] mazeGrid_Visited;
    Stack<int[]> mazeGenStack;

    // Keep track of the walls.
    int[][] vertical_walls;
    int[][] horizontal_walls;

    private int MazeSize;
    // private bool isMazeGenerated;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public MazeGenerator(int mazeSize)
    {
        // isMazeGenerated = false;
        MazeSize = mazeSize;

        // For maze generation.
        // Accepts int arrays where they are size 2 and represent coordinates in the maze.
        mazeGenStack = new Stack<int[]>();

        // Maze grid, allows us to generate a maze and know which spots are visited and which aren't in the algorithm.
        mazeGrid_Visited = new int[MazeSize][];

        // Wall grid. There are 2n (n+1) walls (see https://sunshine2k.blogspot.com/2014/04/while-implementing-algorithm-for-hobby.html)
        //   n+1 rows of vertical walls
        //   n+1 collumns of horizontal walls
        //   Each row and column has n walls
        //   Explained in detail in GetMaze()
        vertical_walls = new int[MazeSize][];
        horizontal_walls = new int[MazeSize][];


        ClearMaze();
    }

    /** 
     * GetMaze() returns the wall specifications for the current generated maze in this MazeGenerator object.
     * If no maze has been generated yet, then one will automatically be generated.
     * 
     * Explanation of the output:
     * 
     * 
                Each vertical row of walls is one array.
            Each column of walls is an array.
            
            Say this is a grid which will soon be turned into a maze.
             _ _ _ _
            | | | | |
             - - - -
            | | | | |
             - - - -
            | | | | |
             - - - -
            | | | | |
             - - - -
            The following is one vertical column of walls.
             -
             -
             -
             -
             -
            This, along with the three other vertical wall columns, each have an array representing them.
            Same with rows.
            | | | | |
            This row of walls is represented by an array inside of int[][] horizontal_walls.

            So int[][] vertical_walls has the following arrays each of length 5 (MazeSize + 1):
            0) | | | | | 
            1) | | | | | 
            2) | | | | | 
            3) | | | | |

            And int[][] horizontal_walls has as arrays, again each of length MazeSize + 1:
              0) - 1) - 2) - 3) -
                 -    -    -    -
                 -    -    -    -
                 -    -    -    -
                 -    -    -    -

            However, they are not represented by - and |, but by 0s and 1s.
            Where 0 is a wall and 1 is the absence of a wall.
         
     */
    public int[][][] GetMaze()
    {
        // Reset the maze generation fields. The stack is empty, the isVisitedGrid is all zeros, the walls are all set to zero.
        ClearMaze();
        // Use the generation algorithm to make a maze if the maze hasn't been generated yet.
        GenerateMaze();
        // Return the maze in the form of an array holding the wall specifications.
        int[][][] toReturn = new int[2][][];
        toReturn[0] = vertical_walls;
        toReturn[1] = horizontal_walls;
        return toReturn;
    }


    /**
     * GenerateMaze() generates a new maze in this MazeGenerator object using the iterative depth first search algorithm.
     * Use GetMaze() to print out the wall specs for the maze currently generated.
     * 
     */
    public void GenerateMaze()
    {
        System.Random random = new System.Random();

        // Random maze generator, iterative depth first search, see Wikipedia article "Maze Generation Algorithm" section "Iterative implementation (with stack)."

        // Initialize starting position.
        int start_x = random.Next(MazeSize);
        int start_y = random.Next(MazeSize);

        // Mark it as visited and push it to the stack.
        mazeGrid_Visited[start_x][start_y] = 1;
        int[] start_pos = { start_x, start_y };
        mazeGenStack.Push(start_pos);

        while (mazeGenStack.Count > 0)
        {
            // Pop a cell and make it the current cell.
            int[] currentCell = mazeGenStack.Pop();

            // If it has any neighbors that have not been visited.
            ArrayList neighbors = GetNeighbors(currentCell, true);
            if (neighbors.Count > 0)
            {

                // Push currentCell to the stack.
                mazeGenStack.Push(currentCell);

                // Choose one of the unvisited neighbors (I'll choose randomly).
                int randIndex = random.Next(0, neighbors.Count);
                int[] neighborCell = (int[]) neighbors[randIndex];

                // Remove wall between current and chosen cell.
                RemoveWall(currentCell, neighborCell);

                // Mark the chosen cell as visited and push it to the stack.
                mazeGrid_Visited[neighborCell[0]][neighborCell[1]] = 1;
                mazeGenStack.Push(neighborCell);

            }  
        }
    }

    
    // If checkVisit is true, then only add the neighbor if it has been visited.
    private ArrayList GetNeighbors(int[] coords, bool checkVisit)
    {
        int x = coords[0];
        int y = coords[1];

        ArrayList possibleNeighbors = new ArrayList();

        // Four possible neighbors, must check if they are inbounds and visited. If so, add them to the arraylist.

        // If this neighbor is inbounds.
        if (x < MazeSize - 1)
        {
            int[] toAdd_0 = { x + 1, y };

            // If this neighbor has not been visited (skip this check according to the boolean).
            if (!checkVisit) possibleNeighbors.Add(toAdd_0);
            else if (mazeGrid_Visited[toAdd_0[0]][toAdd_0[1]] == 0) possibleNeighbors.Add(toAdd_0);
        }
        if (x > 0)
        {
            int[] toAdd_1 = { x - 1, y };
            if (!checkVisit) possibleNeighbors.Add(toAdd_1);
            else if (mazeGrid_Visited[toAdd_1[0]][toAdd_1[1]] == 0) possibleNeighbors.Add(toAdd_1);
        }
        if (y < MazeSize -1)
        {
            int[] toAdd_2 = { x, y + 1};
            if (!checkVisit) possibleNeighbors.Add(toAdd_2);
            else if (mazeGrid_Visited[toAdd_2[0]][toAdd_2[1]] == 0) possibleNeighbors.Add(toAdd_2);
        }
        if (y > 0)
        {
            int[] toAdd_3 = { x, y - 1};
            if (!checkVisit) possibleNeighbors.Add(toAdd_3);
            else if (mazeGrid_Visited[toAdd_3[0]][toAdd_3[1]] == 0) possibleNeighbors.Add(toAdd_3);
        }
        return possibleNeighbors;
    }

    private void RemoveWall(int[] cell1, int[] cell2)
    {
        // Are they of size 2?
        if (cell1.Length != cell2.Length && cell1.Length != 2)
        {
            throw new Exception("GenerateMaze_p.RemoveWall() was called but the int[] are not both size 2 where each represents x and y coords.");
        }
        
        // Are they valid cells? Check if all four numbers are above -1 and below the mazeSize
        if (cell1[0] < 0 || cell1[1] < 0 || cell1[0] > MazeSize || cell1[1] > MazeSize ||
            cell2[0] < 0 || cell2[1] < 0 || cell2[0] > MazeSize || cell2[1] > MazeSize)
                throw new Exception("GenerateMaze_p.RemoveWall() was called but one of the cells is not valid.") ;
        
        // Are they the same cell?
        if (cell1[0] == cell2[0] && cell1[1] == cell2[1])
            throw new Exception("GenerateMaze_p.RemoveWall() was called but they are the same cell.");

        // Are they not neighbors?
        ArrayList neighbors = GetNeighbors(cell1, false);
        bool neighbor_error_bool = true;
        foreach (var item in neighbors)
        {
            int[] neighbor = (int[]) item;
            if (neighbor[0] == cell2[0] && neighbor[1] == cell2[1]) neighbor_error_bool = false;
        }
        if (neighbor_error_bool) throw new Exception("GenerateMaze_p.RemoveWall() was called but the input cells are not neighbors."); ;


        // Are they in the same row? Remove a horizontal wall.
        if (cell1[0] == cell2[0])
        {
            int horizontalToRemove = Mathf.Max(cell1[1], cell2[1]);
            horizontal_walls[cell1[0]][horizontalToRemove] = 0;
        }

        // Are they in the same column? Remove a vertical wall.
        if (cell1[1] == cell2[1])
        {
            int verticalToRemove = Mathf.Max(cell1[0], cell2[0]);
            vertical_walls[cell1[1]][verticalToRemove] = 0;  // double check this
        }
    }

    private void ClearMaze()
    {
        // For each row in the maze, insert a column of size mazeSize.
        // Zero represented not visited, 1 represents visited.
        for (int i = 0; i < MazeSize; i++)
        {
            mazeGrid_Visited[i] = new int[MazeSize];
        }

        // Return all the walls to the maze.
        // 1 represents a wall and 0 represents no wall.
        for (int i=0; i < MazeSize; i++)
        {
            vertical_walls[i] = new int[MazeSize + 1];
            horizontal_walls[i] = new int[MazeSize + 1];

            for (int j = 0;j < MazeSize + 1; j++)
            {
                vertical_walls[i][j] = 1;
                horizontal_walls[i][j] = 1;
            }
        }

        // Empty the stack.
        mazeGenStack.Clear();
    }
    
    public override string ToString()
    {
        string toReturn = "Maze of size: " + MazeSize + "\n";
        for (int i = 0; i < MazeSize; i++)
        {
            // print the horizontal walls in weird order
            for (int v = 0; v < MazeSize; v++)
            {
                if (horizontal_walls[v][i] == 1) toReturn += "  ";
                else toReturn += " -";
            }
            toReturn += "\n";

            for (int h= 0; h<MazeSize + 1;h++)
            {
                if (vertical_walls[i][h] == 1) toReturn += "  ";
                else toReturn += "| ";
            }
            toReturn += "\n";
            
        }
        for (int v= 0; v < MazeSize;v++)
        {
            if (horizontal_walls[v][MazeSize] == 1) toReturn += "  ";
            else toReturn += " -";
        }
        toReturn += "\n";


        return toReturn;
    }

}
