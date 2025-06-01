//using System.Linq;
//using System.Collections.Generic;
//using UnityEngine;
//using Unity.VisualScripting;
//using Unity.VisualScripting.FullSerializer;

//public class GameManager : MonoBehaviour
//{
//    [SerializeField] private Transform tilePrefab;
//    [SerializeField] private Transform gameHolder;
//    [SerializeField] private Sprite hintTile;


//    private List<Tile> tiles = new();

//    public int width;
//    public int height;
//    public int numMines;
//    public int hintTiles = 2;
//    int avHints = 0;
//    int[] gridTiles;

//    private readonly float tileSize = 0.5f;

//    private void Awake()
//    {
//        width = OptionsMenu.OptionsData.width;
//        height = OptionsMenu.OptionsData.height;
//        numMines = OptionsMenu.OptionsData.numMines;
//        avHints = OptionsMenu.OptionsData.hints;
//    }

//    // Start is called before the first frame update
//    void Start()
//    {
//        CreateGameBoard(width, height, numMines);
//        gridTiles = Enumerable.Range(0, tiles.Count).OrderBy(x => Random.Range(0.0f, 1.0f)).ToArray();
//        ResetGameState();
//        if (OptionsMenu.OptionsData.hintsEnabled)
//        {
//            HintTiles();
//        }
//    }

//    public void CreateGameBoard(int width, int height, int numMines)
//    {
//        // Save the game parameters we're using.
//        this.width = width;
//        this.height = height;
//        float percent = Mathf.Clamp(numMines, 0, 100) / 100f;
//        this.numMines = Mathf.RoundToInt(percent * width * height);
//        print(this.numMines);

//        // Create the array of tiles.
//        for (int row = 0; row < height; row++)
//        {
//            for (int col = 0; col < width; col++)
//            {
//                // Position the tile in the correct place (centred).
//                Transform tileTransform = Instantiate(tilePrefab);
//                tileTransform.parent = gameHolder;
//                float xIndex = col - ((width - 1) / 2.0f);
//                float yIndex = row - ((height - 1) / 2.0f);
//                tileTransform.localPosition = new Vector2(xIndex * tileSize, yIndex * tileSize);
//                // Keep a reference to the tile for setting up the game.
//                Tile tile = tileTransform.GetComponent<Tile>();
//                tiles.Add(tile);
//                tile.gameManager = this;
//            }
//        }
//    }

//    private void ResetGameState()
//    {

//        // Set mines at the first numMines positions.
//        for (int i = 0; i < numMines; i++)
//        {
//            int pos = gridTiles[i];
//            tiles[pos].isMine = true;
//        }

//        // Update all the tiles to hold the correct number of mines.
//        for (int i = 0; i < tiles.Count; i++)
//        {
//            tiles[i].mineCount = HowManyMines(i);
//        }
//    }

//    private void HintTiles()
//    {
//        // Set hints at the first hints positions.
//        try
//        {
//            for (int i = 0; i < avHints; i++)
//            {
//                int pos = gridTiles[i];
//                if (!tiles[pos].isMine)
//                {
//                    tiles[pos].isHint = true;
//                    tiles[pos].GetComponent<SpriteRenderer>().sprite = hintTile;
//                }
//                if (tiles[pos].isMine)
//                {
//                    avHints += 1;
//                    if (avHints < OptionsMenu.OptionsData.hints)
//                    {
//                        avHints = OptionsMenu.OptionsData.hints;
//                    }
//                }

//            }
//        }
//        catch
//        {
//            return;
//        }


//    }


//    // Given a location work out how many mines are surrounding it.
//    private int HowManyMines(int location)
//    {
//        int count = 0;
//        foreach (int pos in GetNeighbours(location))
//        {
//            if (tiles[pos].isMine)
//            {
//                count++;
//            }
//        }
//        return count;
//    }

//    // Given a position, return the positions of all neighbours.
//    private List<int> GetNeighbours(int pos)
//    {
//        List<int> neighbours = new();
//        int row = pos / width;
//        int col = pos % width;
//        // (0,0) is bottom left.
//        if (row < (height - 1))
//        {
//            neighbours.Add(pos + width); // North
//            if (col > 0)
//            {
//                neighbours.Add(pos + width - 1); // North-West
//            }
//            if (col < (width - 1))
//            {
//                neighbours.Add(pos + width + 1); // North-East
//            }
//        }
//        if (col > 0)
//        {
//            neighbours.Add(pos - 1); // West
//        }
//        if (col < (width - 1))
//        {
//            neighbours.Add(pos + 1); // East
//        }
//        if (row > 0)
//        {
//            neighbours.Add(pos - width); // South
//            if (col > 0)
//            {
//                neighbours.Add(pos - width - 1); // South-West
//            }
//            if (col < (width - 1))
//            {
//                neighbours.Add(pos - width + 1); // South-East
//            }
//        }
//        return neighbours;
//    }

//    public void ClickNeighbours(Tile tile)
//    {
//        int location = tiles.IndexOf(tile);
//        foreach (int pos in GetNeighbours(location))
//        {
//            tiles[pos].ClickedTile();
//        }
//    }
//    public void GameOver()
//    {
//        foreach (Tile tile in tiles)
//        {
//            tile.ShowGameOverState();
//        }
//    }
//    public void CheckGameOver()
//    {
//        //if there are numMines left active then we're done
//        int count = 0;
//        foreach (Tile tile in tiles)
//        {
//            if (tile.active)
//            {
//                count++;
//            }
//        }
//        if (count == numMines)
//        {
//            //flag and disable everything, we're done
//            Debug.Log("Winner");
//            foreach (Tile tile in tiles)
//            {
//                tile.active = false;
//                tile.SetFlaggedIfMine();
//            }
//        }

//    }
//    //check on all surrounding tiles if mines are all flagged
//    public void ExpandIfFlagged(Tile tile)
//    {
//        int location = tiles.IndexOf(tile);
//        //get the number of flags
//        int flag_count = 0;

//        foreach (int pos in GetNeighbours(location))
//        {
//            if (tiles[pos].flagged)
//            {
//                flag_count++;
//            }
//        }
//        //if we have the right number clicks surrounding tiles
//        if (flag_count == tile.mineCount)
//        {
//            //clicking a flag does nothing so this is safe
//            ClickNeighbours(tile);
//        }
//    }
//}
