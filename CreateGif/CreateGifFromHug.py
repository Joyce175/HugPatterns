import numpy as np
import matplotlib.pyplot as plt
from PIL import Image
import io
import os

input_folder = "input"
output_folder = "output"

# Ensure the output folder exists
os.makedirs(output_folder, exist_ok=True)

# Get a list of text files in the input folder
input_files = [f for f in os.listdir(input_folder) if f.endswith(".txt")]

# Initialize the data_arrays list
data_arrays = []

# Process each input file
for input_file in input_files:
    input_file_path = os.path.join(input_folder, input_file)
    
    # Read the lines from the input file
    with open(input_file_path, 'r') as file:
        lines = file.readlines()

    # Process lines with a delay
    for line in lines:
        # Remove the newline character and split values by commas
        values = line.strip().split(',')

        # Remove the last value from the list
        values.pop()

        # Convert values to integers
        values = [int(val) for val in values]

        # Convert the 1D list to a 2D array of shape (4, 4)
        array_2d = np.array(values).reshape(4, 4)

        # Append the array to data_arrays
        data_arrays.append(array_2d)

# Normalize each data array to the range [0, 1]
normalized_data_arrays = [data_array / 255.0 for data_array in data_arrays]

# Create a GIF by saving each frame
frames = []
for normalized_data in normalized_data_arrays:
    plt.imshow(normalized_data, cmap="YlGnBu", vmin=0, vmax=1)
    plt.axis("off")
    plt.colorbar()
    plt.subplots_adjust(left=0, right=1, top=1, bottom=0)  # Remove extra margins
    plt.gca().xaxis.set_major_locator(plt.NullLocator())  # Remove x-axis ticks
    plt.gca().yaxis.set_major_locator(plt.NullLocator())  # Remove y-axis ticks

    # Save the current plot as an image
    buffer = io.BytesIO()
    plt.savefig(buffer, format="png", bbox_inches="tight", pad_inches=0, dpi=100)
    buffer.seek(0)
    image = Image.open(buffer)
    frames.append(image)
    plt.close()

# Save the frames as a GIF
frames[0].save("output.gif", save_all=True, append_images=frames[1:], loop=0, duration=130)