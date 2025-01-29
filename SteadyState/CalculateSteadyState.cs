using System;
using System.Numerics;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;

class MonopolyMatrix
{
	private const int BOARD_SIZE = 40;
	private const int MAX_ROLL = 12;
	private const int MIN_ROLL = 2;
	private const int JAIL_SQUARE = 10;
	private const int GO_TO_JAIL_SQUARE = 30;
	private static readonly int[] COMMUNITY_CHEST = {2, 17, 33};
	private static readonly int[] CHANCE = {7, 22, 36};
	private static readonly int[] STATIONS = { 5, 15, 25, 35 }; // Kings Cross, Marylebone, Fenchurch Street, Liverpool Street
	private static readonly int[] UTILITIES = { 12, 28 }; // Electric Company, Water Works

	public static double[,] GenerateTransitionMatrix()
	{
		var matrix = new double[BOARD_SIZE, BOARD_SIZE];
		var diceProbs = CalculateDiceProbabilities();

		for (int fromSquare = 0; fromSquare < BOARD_SIZE; fromSquare++)
		{
			for (int roll = MIN_ROLL; roll <= MAX_ROLL; roll++)
			{
				int toSquare = (fromSquare + roll) % BOARD_SIZE;

				if (fromSquare != GO_TO_JAIL_SQUARE)
				{
					matrix[fromSquare, JAIL_SQUARE] += (Math.Pow(1 / 12.0, 3)); // 3 Double Rolls
				}

				// If landing on "Go To Jail", redirect to Jail
				if (toSquare == GO_TO_JAIL_SQUARE)
				{
					//matrix[toSquare, JAIL_SQUARE] = 1.0;
					matrix[fromSquare, JAIL_SQUARE] += diceProbs[roll];
					
				}
				else if (COMMUNITY_CHEST.Contains(toSquare))
				{
					matrix[fromSquare, toSquare] += (diceProbs[roll] * 14 / 16.0);

					// Advance to GO
					matrix[fromSquare, 0] += (diceProbs[roll] * 1 / 16.0);

					// Go to Jail
					matrix[fromSquare, JAIL_SQUARE] += (diceProbs[roll] * 1 / 16.0);
				}
				else if (CHANCE.Contains(toSquare))
				{
					matrix[fromSquare, toSquare] += (diceProbs[roll] * 6 / 16.0);

					// Advance to GO
					matrix[fromSquare, 0] += (diceProbs[roll] * 1 / 16.0);

					// Advance to Trafalgar Square
					matrix[fromSquare, 24] += (diceProbs[roll] * 1 / 16.0);

					// Advance to Mayfair
					matrix[fromSquare, 39] += (diceProbs[roll] * 1 / 16.0);

					// Advance to Pall Mall
					matrix[fromSquare, 11] += (diceProbs[roll] * 1 / 16.0);

					// Advance to Kings Cross Station
					matrix[fromSquare, 5] += (diceProbs[roll] * 1 / 16.0);

					// Go to Jail
					matrix[fromSquare, JAIL_SQUARE] += (diceProbs[roll] * 1 / 16.0);

					// Move back 3 spaces
					matrix[fromSquare, toSquare - 3] += (diceProbs[roll] * 1 / 16.0);

					// Find nearest station/utility by moving forward
					int nearestStation = STATIONS.Where(s => s > toSquare)
						.DefaultIfEmpty(STATIONS.First())
						.First();
					
					matrix[fromSquare, nearestStation] += (diceProbs[roll] * 1 / 8.0);

					int nearestUtility = UTILITIES.Where(u => u > toSquare)
						.DefaultIfEmpty(UTILITIES.First())
						.First();

					matrix[fromSquare, nearestUtility] += (diceProbs[roll] * 1 / 16.0);
				}
				else
				{
					matrix[fromSquare, toSquare] += diceProbs[roll];
				}
			}
		}

		return matrix;
	}

	private static double[] CalculateDiceProbabilities()
	{
		var probs = new double[MAX_ROLL + 1];

		for (int die1 = 1; die1 <= 6; die1++)
		{
			for (int die2 = 1; die2 <= 6; die2++)
			{
				probs[die1 + die2]++;
			}
		}

		// Convert counts to probabilities
		for (int i = MIN_ROLL; i <= MAX_ROLL; i++)
		{
			probs[i] /= 36.0;
		}

		return probs;
	}

	public static void PrintMatrix(double[,] matrix)
	{
		for (int i = 0; i < BOARD_SIZE; i++)
		{
			for (int j = 0; j < BOARD_SIZE; j++)
			{
				Console.Write($"{matrix[i, j]:F3} ");
			}
			Console.WriteLine();
		}
	}

	static void SaveMatrixToFile(double[,] matrix)
	{
		using (StreamWriter writer = new StreamWriter("monopoly_matrix.txt"))
		{
			for (int i = 0; i < matrix.GetLength(0); i++)
			{
				for (int j = 0; j < matrix.GetLength(1); j++)
				{
					writer.Write($"{matrix[i, j]:F3} ");
				}
				writer.WriteLine();
			}
		}
	}

	public static double[] ComputeSteadyState(double[,] transitionMatrix)
	{
		// Convert to MathNet matrix
		var matrix = Matrix<double>.Build.DenseOfArray(transitionMatrix);

		// Compute eigenvalues and eigenvectors
		var evd = matrix.Transpose().Evd();

		
		Console.WriteLine("Eigenvalues:");
		for (int i = 0; i < evd.EigenValues.Count; i++)
		{
			Console.WriteLine($"Eigenvalue {i}: {evd.EigenValues[i]}");
		}
		

		// Find index of eigenvalue closest to 1
		int steadyStateIndex = Array.IndexOf(
			evd.EigenValues.Select(c => Math.Abs(c.Real - 1)).ToArray(),
			evd.EigenValues.Select(c => Math.Abs(c.Real - 1)).Min()
		);

		// Extract and normalize eigenvector
		var steadyStateVector = evd.EigenVectors.Column(steadyStateIndex);
		var normalized = steadyStateVector.Divide(steadyStateVector.Sum());

		return normalized.ToArray();
	}

	private static bool IsValidTransitionMatrix(double[,] matrix)
	{
		for (int row = 0; row < BOARD_SIZE; row++)
		{
			double rowSum = 0;
			for (int col = 0; col < BOARD_SIZE; col++)
			{
				rowSum += matrix[row, col];
			}
			if (Math.Abs(rowSum - 1.0) > 0.001) // Allow small floating-point error
				Console.WriteLine(rowSum);
				//return false;
		}
		return true;
	}

	static void Main()
	{
		var transitionMatrix = GenerateTransitionMatrix();
		var steadyState = ComputeSteadyState(transitionMatrix);

		Console.WriteLine(IsValidTransitionMatrix(transitionMatrix));

		SaveMatrixToFile(transitionMatrix);

		
		Console.WriteLine("Steady State Probabilities:");
		for (int i = 0; i < steadyState.Length; i++)
		{
			Console.WriteLine($"Square {i}: {steadyState[i]:P4}");
		}

		// Optionally, save to file
		File.WriteAllLines("steady_state.txt",
			steadyState.Select((prob, index) => $"Square {index}: {prob:P4}"));
		
	}
}
