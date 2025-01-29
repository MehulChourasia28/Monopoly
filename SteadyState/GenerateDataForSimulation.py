import random
import numpy as np
import pandas as pd
from collections import defaultdict

class MonopolySimulation:
    # Board constants
    BOARD_SIZE = 40
    MAX_ROLL = 12
    MIN_ROLL = 2
    JAIL_SQUARE = 10
    GO_TO_JAIL_SQUARE = 30
    COMMUNITY_CHEST = [2, 17, 33]
    CHANCE = [7, 22, 36]
    STATIONS = [5, 15, 25, 35]  # Kings Cross, Marylebone, Fenchurch Street, Liverpool Street
    UTILITIES = [12, 28]  # Electric Company, Water Works

    def __init__(self):
        self.board_spaces = [
            "GO", "Old Kent Road", "Community Chest 1", "Whitechapel Road", "Income Tax",
            "Kings Cross Station", "The Angel Islington", "Chance 1", "Euston Road", "Pentonville Road",
            "Jail", "Pall Mall", "Electric Company", "Whitehall", "Northumberland Avenue",
            "Marylebone Station", "Bow Street", "Community Chest 2", "Marlborough Street", "Vine Street",
            "Free Parking", "Strand", "Chance 2", "Fleet Street", "Trafalgar Square",
            "Fenchurch St Station", "Leicester Square", "Coventry Street", "Water Works", "Piccadilly",
            "Go To Jail", "Regent Street", "Oxford Street", "Community Chest 3", "Bond Street",
            "Liverpool St Station", "Chance 3", "Park Lane", "Super Tax", "Mayfair"
        ]
        self.current_position = 0
        self.landings = defaultdict(int)
        self.move_history = []
        self.transition_matrix = self.generate_transition_matrix()
        self.moves_count = 0  # Add counter for moves

    @staticmethod
    def calculate_dice_probabilities():
        """Calculate the probability distribution for rolling two dice"""
        probs = [0] * (MonopolySimulation.MAX_ROLL + 1)
        for die1 in range(1, 7):
            for die2 in range(1, 7):
                probs[die1 + die2] += 1
        
        # Convert counts to probabilities
        for i in range(MonopolySimulation.MIN_ROLL, MonopolySimulation.MAX_ROLL + 1):
            probs[i] /= 36.0
        
        return probs

    def generate_transition_matrix(self):
        """Generate the transition probability matrix for the Monopoly board"""
        matrix = np.zeros((self.BOARD_SIZE, self.BOARD_SIZE))
        dice_probs = self.calculate_dice_probabilities()

        for from_square in range(self.BOARD_SIZE):
            for roll in range(self.MIN_ROLL, self.MAX_ROLL + 1):
                to_square = (from_square + roll) % self.BOARD_SIZE

                # Add probability for three consecutive doubles leading to jail
                if from_square != self.GO_TO_JAIL_SQUARE:
                    matrix[from_square][self.JAIL_SQUARE] += pow(1/12, 3)

                # Handle "Go To Jail" square
                if to_square == self.GO_TO_JAIL_SQUARE:
                    matrix[from_square][self.JAIL_SQUARE] += dice_probs[roll]
                
                # Handle Community Chest
                elif to_square in self.COMMUNITY_CHEST:
                    matrix[from_square][to_square] += (dice_probs[roll] * 14/16)
                    matrix[from_square][0] += (dice_probs[roll] * 1/16)  # Advance to GO
                    matrix[from_square][self.JAIL_SQUARE] += (dice_probs[roll] * 1/16)  # Go to Jail

                # Handle Chance
                elif to_square in self.CHANCE:
                    matrix[from_square][to_square] += (dice_probs[roll] * 6/16)
                    matrix[from_square][0] += (dice_probs[roll] * 1/16)  # Advance to GO
                    matrix[from_square][24] += (dice_probs[roll] * 1/16)  # Advance to Trafalgar Square
                    matrix[from_square][39] += (dice_probs[roll] * 1/16)  # Advance to Mayfair
                    matrix[from_square][11] += (dice_probs[roll] * 1/16)  # Advance to Pall Mall
                    matrix[from_square][5] += (dice_probs[roll] * 1/16)  # Advance to Kings Cross
                    matrix[from_square][self.JAIL_SQUARE] += (dice_probs[roll] * 1/16)  # Go to Jail
                    
                    # Move back 3 spaces
                    back_three = (to_square - 3) % self.BOARD_SIZE
                    matrix[from_square][back_three] += (dice_probs[roll] * 1/16)
                    
                    # Find nearest station
                    nearest_station = next((s for s in self.STATIONS if s > to_square), self.STATIONS[0])
                    matrix[from_square][nearest_station] += (dice_probs[roll] * 1/8)
                    
                    # Find nearest utility
                    nearest_utility = next((u for u in self.UTILITIES if u > to_square), self.UTILITIES[0])
                    matrix[from_square][nearest_utility] += (dice_probs[roll] * 1/16)

                # Handle normal movement
                else:
                    matrix[from_square][to_square] += dice_probs[roll]

        return matrix

    def move(self):
        """Move based on transition probabilities"""
        # Get the probability distribution for the current position
        current_position_probabilities = self.transition_matrix[self.current_position]

        # Ensure probabilities sum to 1 (handle any floating point precision issues)
        current_position_probabilities = current_position_probabilities / np.sum(current_position_probabilities)

        # Choose next position based on probability distribution
        next_position = np.random.choice(range(self.BOARD_SIZE), p=current_position_probabilities)

        self.current_position = next_position
        space = self.board_spaces[self.current_position]
        self.landings[space] += 1

        # Increment moves counter
        self.moves_count += 1

        # Only update history every 10 moves
        if self.moves_count % 100 == 0:
            self.move_history.append(dict(self.landings))

    def save_to_csv(self, filename="monopoly_landings.csv"):
        """Save the landing history to a CSV file"""
        df = pd.DataFrame(self.move_history)
        df = df.fillna(0)
        df = df.reindex(sorted(df.columns), axis=1)
        df.to_csv(filename, index=True)
        return df

def run_simulation(num_moves=100000):
    """Run a Monopoly simulation for a specified number of moves"""
    sim = MonopolySimulation()
    
    for _ in range(num_moves):
        sim.move()
    
    return sim.save_to_csv()

if __name__ == "__main__":
    results_df = run_simulation()
    print(f"Simulation complete! Results saved to monopoly_landings.csv")
    
    final_counts = results_df.iloc[-1].sort_values(ascending=False)
    print("\nFinal landing counts:")
    print(final_counts)
