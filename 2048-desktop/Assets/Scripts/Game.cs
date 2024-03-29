using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class Game : MonoBehaviour
{
    public static int gridWidth = 4, gridHeight = 4;
    public static Transform[,] grid = new Transform[gridWidth, gridHeight];

	public static SavedTile[,] previousGrid = new SavedTile[gridWidth, gridHeight];
	private int previousScore = 0;

	public Canvas gameOverCanvas;

	public Text gameScoreText;
	public Text highScoreText;
	public int score = 0;

	private int numberOfCoroutinesRunning = 0;
	private bool generatedNewTileThisTurn = true;

	public AudioClip moveTilesSound;
	public AudioClip mergeTilesSound;
	private AudioSource audioSource;

	bool madeFirstMove = false;

    // Start is called before the first frame update
    void Start()
    {
		GenerateNewTile(2);

		audioSource = transform.GetComponent<AudioSource>();

		UpdateHighScore();
    }

    // Update is called once per frame
    void Update()
    {
		if(numberOfCoroutinesRunning == 0)
        {
			if(!generatedNewTileThisTurn)
            {
				generatedNewTileThisTurn = true;
				GenerateNewTile(1);
            }

			if (!CheckGameOver())
			{
				CheckUserInput();
			}
			else
			{
				SaveHighScore();
				UpdateScore();
				gameOverCanvas.gameObject.SetActive(true);
			}
		}
    }

    void CheckUserInput()
    {
        bool down = Input.GetKeyDown(KeyCode.DownArrow);
		bool up = Input.GetKeyDown(KeyCode.UpArrow);
		bool left = Input.GetKeyDown(KeyCode.LeftArrow);
		bool right = Input.GetKeyDown(KeyCode.RightArrow);

		if (down || up || left || right)
        {
			if (!madeFirstMove)
            {
				madeFirstMove = true;
            }

			StorePreviousTiles();
			PrepareTilesForMerging();

			if (down)
			{
				MoveAllTiles(Vector2.down);
			}

			if (up)
			{
				MoveAllTiles(Vector2.up);
			}

			if (left)
			{
				MoveAllTiles(Vector2.left);
			}

			if (right)
			{
				MoveAllTiles(Vector2.right);
			}
		}
    }

    private void StorePreviousTiles()
    {
		previousScore = score;

        for (int x = 0; x < gridWidth; x++)
        {
			for (int y = 0; y < gridHeight; y++)
            {
				Transform tempTile = grid[x, y];
				previousGrid[x, y] = null;
				
				if(tempTile != null)
                {
					SavedTile savedTile = new SavedTile();

					savedTile.location = tempTile.localPosition;
					savedTile.value = tempTile.GetComponent<Tile>().tileValue;

					previousGrid[x, y] = savedTile;
				}
            }
        }
    }

    void UpdateScore()
    {
		gameScoreText.text = score.ToString("000000000");
    }

	void UpdateHighScore()
    {
		highScoreText.text = PlayerPrefs.GetInt("highscore").ToString("000000000");
    }

	void SaveHighScore()
    {
		int oldHighScore = PlayerPrefs.GetInt("highscore");
		if(score > oldHighScore)
        {
			PlayerPrefs.SetInt("highscore", score);
        }
    }

	bool CheckGameOver()
    {
		
		if (transform.childCount < gridWidth * gridHeight)
        {
            return false;
        }

        for (int x = 0; x < gridWidth; x++)
        {
			for (int y = 0; y < gridHeight; y++)
            {
				Transform currentTile = grid[x, y];
				Transform tileBelow = null;
				Transform tileBeside = null;

				if (y != 0)
                {
					tileBelow = grid[x, y - 1];
				}
				if (x != gridWidth - 1)
                {
					tileBeside = grid[x + 1, y];
				}

				if (tileBeside != null)
                {
                    if (currentTile.GetComponent<Tile>().tileValue == tileBeside.GetComponent<Tile>().tileValue)
                    {
                        return false;
                    }
                }

                if (tileBelow != null)
                {
                    if (currentTile.GetComponent<Tile>().tileValue == tileBelow.GetComponent<Tile>().tileValue)
                    {
                        return false;
                    }
                }
            }
        }
		return true;
    }
	
	void MoveAllTiles (Vector2 direction)
    {
		int tilesMovedCount = 0;
		UpdateGrid();

		if (direction == Vector2.left)
        {
			for (int x = 0; x < gridWidth; x++)
            {
				for ( int y = 0; y < gridHeight; y++)
                {
					if(grid[x,y] != null)
                    {
						if(MoveTile(grid[x,y], direction))
                        {
							tilesMovedCount++;
                        }
                    }
                }
            }
        }
		if (direction == Vector2.right)
		{
			for (int x = gridWidth - 1; x >= 0; x--)
			{
				for (int y = 0; y < gridHeight; y++)
				{
					if (grid[x, y] != null)
					{
						if (MoveTile(grid[x, y], direction))
						{
							tilesMovedCount++;
						}
					}
				}
			}
		}
		if (direction == Vector2.down)
		{
			for (int x = 0; x < gridWidth; x++)
			{
				for (int y = 0; y < gridHeight; y++)
				{
					if (grid[x, y] != null)
					{
						if (MoveTile(grid[x, y], direction))
						{
							tilesMovedCount++;
						}
					}
				}
			}
		}
		if (direction == Vector2.up)
		{
			for (int x = 0; x < gridWidth; x++)
			{
				for (int y = gridHeight - 1; y >= 0; y--)
				{
					if (grid[x, y] != null)
					{
						if (MoveTile(grid[x, y], direction))
						{
							tilesMovedCount++;
						}
					}
				}
			}
		}

		if(tilesMovedCount != 0)
        {
			generatedNewTileThisTurn = false;

			audioSource.PlayOneShot(moveTilesSound);
        }

		for (int y = 0; y < gridHeight; y++)
        {
			for(int x = 0; x < gridWidth; x++)
            {
				if(grid[x,y] != null)
                {
					Transform t = grid[x, y];
					StartCoroutine(SlideTile(t.gameObject, 10f));
				}
            }
        }
	}

	bool MoveTile(Transform tile, Vector2 direction)
    {
		Vector2 startPos = tile.localPosition;
		Vector2 phantomTilePosition = tile.localPosition;

		tile.GetComponent<Tile>().startingPosition = startPos;

		while (true)
        {
			phantomTilePosition += direction;
			Vector2 previousPosition = phantomTilePosition - direction;

			if (CheckIsInsideGrid(phantomTilePosition))
            {
				if(CheckIsAtValidPosition(phantomTilePosition))
                {
					tile.GetComponent<Tile>().moveToPosition = phantomTilePosition;
					grid[(int)previousPosition.x, (int)previousPosition.y] = null;
					grid[(int)phantomTilePosition.x, (int)phantomTilePosition.y] = tile;
				}
				else
                {
					if(!CheckAndCombineTiles(tile, phantomTilePosition, previousPosition))
                    {
						phantomTilePosition -= direction;
						tile.GetComponent<Tile>().moveToPosition = phantomTilePosition;

						if (phantomTilePosition == startPos)
                        {
							return false;
						}
						else
                        {
							return true;
						}
					}
				}
            }
            else
            {
				phantomTilePosition -= direction;
				tile.GetComponent<Tile>().moveToPosition = phantomTilePosition;

				if (phantomTilePosition == startPos)
                {
					return false;
				}
				else
                {
					return true;
				}
            }
        }
    }

	bool CheckAndCombineTiles(Transform movingTile, Vector2 phantomTilePosition, Vector2 previousPosition)
    {
		Vector2 pos = movingTile.transform.localPosition;
		Transform collidingTile = grid[(int)phantomTilePosition.x, (int)phantomTilePosition.y];

		int movingTileValue = movingTile.GetComponent<Tile>().tileValue;
		int collidingTileValue = collidingTile.GetComponent<Tile>().tileValue;

		if(movingTileValue == collidingTileValue && !movingTile.GetComponent<Tile>().mergedThisTurn && !collidingTile.GetComponent<Tile>().mergedThisTurn && !collidingTile.GetComponent<Tile>().willMergeWithCollidingTile)
        {
			movingTile.GetComponent<Tile>().destroyMe = true;
			movingTile.GetComponent<Tile>().collidingTile = collidingTile;
			movingTile.GetComponent<Tile>().moveToPosition = phantomTilePosition;
			grid[(int)previousPosition.x, (int)previousPosition.y] = null;
			grid[(int)phantomTilePosition.x, (int)phantomTilePosition.y] = movingTile;
			movingTile.GetComponent<Tile>().willMergeWithCollidingTile = true;

			UpdateScore();

			return true;
        }
		return false;
	}

	void GenerateNewTile(int tileCount)
    {
		for (int i = 0; i < tileCount; i++)
        {
			Vector2 newTileLocation = GetRandomLocationForNewTile();
			string tile = "tile_2";

			float chanceOfTwo = Random.Range(0f, 1f);
			if(chanceOfTwo > 0.9f)
            {
				tile = "tile_4";
            }
			
			GameObject newTile = (GameObject)Instantiate(Resources.Load(tile, typeof(GameObject)), newTileLocation, Quaternion.identity);
			newTile.transform.parent = transform;

			grid[(int)newTile.transform.localPosition.x, (int)newTile.transform.localPosition.y] = newTile.transform;
			newTile.transform.localScale = new Vector2(0, 0);
			newTile.transform.localPosition = new Vector2(newTile.transform.localPosition.x + 0.5f, newTile.transform.localPosition.y + 0.5f);

			StartCoroutine(NewTilePopIn(newTile, new Vector2(0, 0), new Vector2(1, 1), 10f, newTile.transform.localPosition, new Vector2(newTile.transform.localPosition.x - 0.5f, newTile.transform.localPosition.y - 0.5f)));

		}
    }

	void UpdateGrid()
    {
		for (int y = 0; y < gridHeight; y++)
        {
			for (int x = 0; x < gridWidth; x++)
            {
				if (grid[x,y] != null)
                {
					if (grid[x,y].parent == transform)
                    {
						grid[x,y] = null;
                    }
                }
            }
        }
        foreach (Transform tile in transform)
        {
			Vector2 v = new Vector2(Mathf.Round(tile.position.x), Mathf.Round(tile.position.y));
			grid[(int)v.x, (int)v.y] = tile;
        }
    }

	Vector2 GetRandomLocationForNewTile()
	{
		List<int> x = new List<int>();
		List<int> y = new List<int>();
		
		for (int j = 0; j < gridWidth; j++) 
		{
			for (int i = 0; i < gridHeight; i++) 
			{
				if (grid[j,i] == null) 
				{
					x.Add(j);
					y.Add(i);
				}
			}
		}
		
		int randIndex = Random.Range(0, x.Count);
		int randX = x.ElementAt(randIndex);
		int randY = y.ElementAt(randIndex);

		return new Vector2(randX, randY);
	}
	
	bool CheckIsInsideGrid(Vector2 pos)
    {
		if (pos.x >= 0 && pos.x <= gridWidth - 1 && pos.y >= 0 && pos.y <= gridHeight - 1)
        {
			return true;
        }
		return false;
    }
	
	bool CheckIsAtValidPosition(Vector2 pos)
    {
		if (grid[(int)pos.x, (int)pos.y] == null)
        {
			return true;
        }
		return false;
    }

	void PrepareTilesForMerging ()
    {
		foreach (Transform t in transform)
        {
			t.GetComponent<Tile>().mergedThisTurn = false;
        }
    }
	
	public void PlayAgain()
	{
		grid = new Transform[gridWidth, gridHeight];
		SaveHighScore();
		score = 0;

		List<GameObject> children = new List<GameObject>();
		foreach (Transform t in transform)
        {
			children.Add(t.gameObject);
        }
		children.ForEach(t => DestroyImmediate(t));

		gameOverCanvas.gameObject.SetActive(false);
		UpdateScore();
		UpdateHighScore();
		GenerateNewTile(2);
	}

	public void Undo()
    {
		if (madeFirstMove)
        {
			score = previousScore;
			UpdateScore();

			foreach (Transform child in transform)
			{
				Destroy(child.gameObject);
			}
			for (int x = 0; x < gridWidth; x++)
			{
				for (int y = 0; y < gridHeight; y++)
				{
					grid[x, y] = null;

					SavedTile savedTile = previousGrid[x, y];
					if (savedTile != null)
					{
						int tileValue = savedTile.value;
						string newTileName = "tile_" + tileValue;
						GameObject newTile = (GameObject)Instantiate(Resources.Load(newTileName, typeof(GameObject)), savedTile.location, Quaternion.identity);
						newTile.transform.parent = transform;
						grid[x, y] = newTile.transform;
					}
				}
			}
		}
    }

	IEnumerator NewTilePopIn(GameObject tile, Vector2 initialScale, Vector2 finalScale, float timeScale, Vector2 initialPosition, Vector2 finalPosition)
    {
		numberOfCoroutinesRunning++;

		float progress = 0;
		while(progress <= 1)
        {
			tile.transform.localScale = Vector2.Lerp(initialScale, finalScale, progress);
			tile.transform.localPosition = Vector2.Lerp(initialPosition, finalPosition, progress);
			progress += Time.deltaTime * timeScale;
			yield return null;
        }
		tile.transform.localScale = finalScale;
		tile.transform.localPosition = finalPosition;

		numberOfCoroutinesRunning--;
    }

	IEnumerator SlideTile(GameObject tile, float timeScale)
    {
		numberOfCoroutinesRunning++;

		float progress = 0;
		while(progress <= 1)
        {
			tile.transform.localPosition = Vector2.Lerp(tile.GetComponent<Tile>().startingPosition, tile.GetComponent<Tile>().moveToPosition, progress);
			progress += Time.deltaTime * timeScale;
			yield return null;
        }
		tile.transform.localPosition = tile.GetComponent<Tile>().moveToPosition;

		if(tile.GetComponent<Tile>().destroyMe)
        {
			int movingTileValue = tile.GetComponent<Tile>().tileValue;
			if (tile.GetComponent<Tile>().collidingTile != null)
            {
				DestroyImmediate(tile.GetComponent<Tile>().collidingTile.gameObject);
            }
			Destroy(tile.gameObject);

			string newTileName = "tile_" + movingTileValue * 2;

			score += movingTileValue * 2;
			UpdateScore();

			audioSource.PlayOneShot(mergeTilesSound);

			GameObject newTile = (GameObject)Instantiate(Resources.Load(newTileName, typeof(GameObject)), tile.transform.localPosition, Quaternion.identity);
			newTile.transform.parent = transform;
			newTile.GetComponent<Tile>().mergedThisTurn = true;
			grid[(int)newTile.transform.localPosition.x, (int)newTile.transform.localPosition.y] = newTile.transform;
			newTile.transform.localScale = new Vector2(0, 0);
			newTile.transform.localPosition = new Vector2(newTile.transform.localPosition.x + 0.5f, newTile.transform.localPosition.y + 0.5f);

			yield return StartCoroutine(NewTilePopIn(newTile, new Vector2(0, 0), new Vector2(1, 1), 10f, newTile.transform.localPosition, new Vector2(newTile.transform.localPosition.x - 0.5f, newTile.transform.localPosition.y - 0.5f)));
        }

		numberOfCoroutinesRunning--;
    }
}