import tensorflow as tf

# Create a simple Sequential model
model = tf.keras.models.Sequential([
    tf.keras.layers.InputLayer(input_shape=(10,)),  # Correctly specify the input layer
    tf.keras.layers.Dense(32, activation='relu'),
    tf.keras.layers.Dense(1, activation='sigmoid')
])

# Compile the model
model.compile(optimizer='adam', loss='binary_crossentropy')

# Save the model to a file
model.save("test.h5")
