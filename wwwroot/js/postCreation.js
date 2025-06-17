const uploadBox = document.querySelector(".upload-box"),
previewImg = uploadBox.querySelector("img"),
fileInput = uploadBox.querySelector("input"),
widthInput = document.querySelector(".width input"),
heightInput = document.querySelector(".height input"),
aspectRatioCheckbox = document.querySelector("#aspect-ratio-checkbox");

const MAX_WIDTH = 800; // Maximum allowed width

const loadFile = (e) => {
    const file = e.target.files[0]; 
    if (!file) return;

    previewImg.src = URL.createObjectURL(file); 
    previewImg.addEventListener("load", () => {
        let originalWidth = previewImg.naturalWidth;
        let originalHeight = previewImg.naturalHeight;

        // Apply max width constraint
        if (originalWidth > MAX_WIDTH) {
            let scaleFactor = MAX_WIDTH / originalWidth;
            originalWidth = MAX_WIDTH;
            originalHeight = Math.floor(originalHeight * scaleFactor);
        }

        widthInput.value = originalWidth;
        heightInput.value = originalHeight;
        previewImg.style.width = `${originalWidth}px`;
        previewImg.style.height = `${originalHeight}px`;

        document.querySelector(".wrapper")?.classList.add("active");
    });
};

fileInput.addEventListener("change", loadFile);
uploadBox.addEventListener("click", () => fileInput.click());

const resizeImage = () => {
    if (!previewImg.src) return;

    let newWidth = parseInt(widthInput.value) || previewImg.naturalWidth;
    let newHeight = parseInt(heightInput.value) || previewImg.naturalHeight;
    let aspectRatio = previewImg.naturalWidth / previewImg.naturalHeight;

    if (aspectRatioCheckbox.checked) {
        newHeight = Math.floor(newWidth / aspectRatio); // Maintain aspect ratio
    }

    // Prevent width exceeding MAX_WIDTH
    if (newWidth > MAX_WIDTH) {
        newWidth = MAX_WIDTH;
        newHeight = aspectRatioCheckbox.checked ? Math.floor(newWidth / aspectRatio) : newHeight;
    }

    widthInput.value = newWidth;
    heightInput.value = newHeight;
    previewImg.style.width = `${newWidth}px`;
    previewImg.style.height = `${newHeight}px`;
};

// Apply resizing based on user input
widthInput.addEventListener("input", resizeImage);
heightInput.addEventListener("input", () => {
    if (aspectRatioCheckbox.checked) {
        let aspectRatio = previewImg.naturalWidth / previewImg.naturalHeight;
        widthInput.value = Math.floor(heightInput.value * aspectRatio); // Adjust width automatically
    }
    resizeImage(); // Apply resizing changes
});


aspectRatioCheckbox.addEventListener("change", () => resizeImage());


//the scheduling time and date options 
document.addEventListener("DOMContentLoaded", function() {
    const scheduleOption = document.querySelector('input[name="publish-option"][value="schedule"]');
    const immediateOption = document.querySelector('input[name="publish-option"][value="immediate"]');
    const scheduleContainer = document.getElementById("schedule-container");

    scheduleOption.addEventListener("change", function() {
        scheduleContainer.style.display = "block";
    });

    immediateOption.addEventListener("change", function() {
        scheduleContainer.style.display = "none";
    });
});
