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

//trying to load the tags 
async function loadTags() {
    try {
        const response = await fetch('/api/tags');

        if (!response.ok) {
            throw new Error(`Error loading tags: ${response.statusText}`);
        }

        const tags = await response.json();
        const tagSelect = document.getElementById('post-tags');

        // Clear existing options
        tagSelect.innerHTML = '';
        console.log("Tags received:", tags);

        // Populate new options
        tags.forEach(tag => {
            const option = document.createElement('option');
            option.value = tag.Name || tag.name; // in case it's lowercase
            option.textContent = tag.Name || tag.name;
            tagSelect.appendChild(option);
        });
    } catch (error) {
        console.error('Failed to load tags:', error);
    }
}

//trying to load the tags 
async function loadCategories() {
    try {
        const response = await fetch('/api/categories');

        if (!response.ok) {
            throw new Error(`Error loading tags: ${response.statusText}`);
        }

        const categories = await response.json();
        const categorySelect = document.getElementById('post-categories');

        // Clear existing options
        categorySelect.innerHTML = '';
        console.log("Categories received:", categories);

        // Populate new options
        categories.forEach(category => {
            const option = document.createElement('option');
            option.value = category.Name || category.name; // in case it's lowercase
            option.textContent = category.Name || category.name;
            categorySelect.appendChild(option);
        });
    } catch (error) {
        console.error('Failed to load categories :', error);
    }
}
// Load tags once the DOM is ready
document.addEventListener('DOMContentLoaded', loadTags);
document.addEventListener('DOMContentLoaded', loadCategories);


//saving the post as draft 
document.getElementById("save-draft").addEventListener("click", async function () {
    const form = new FormData(document.getElementById("post-form"));

    try {
        const response = await fetch("/api/posts/draft", {
            method: "POST",
            body: form
        });

        const result = await response.json();
        alert(result.Message || "Draft saved successfully.");
    } catch (err) {
        console.error("Failed to save draft:", err);
        alert("Draft save failed.");
    }
});

//submit the post (create it)
document.getElementById("post-form").addEventListener("submit", async function (e) {
    e.preventDefault();

    const form = new FormData(this);
    const publishOption = form.get("publish-option");
   //this part is related to the scheduling process we must confirm that the time is not past already 
    if (publishOption === "schedule") {
        const dateValue = document.getElementById("publish-date").value;
        const timeValue = document.getElementById("publish-time").value;

        if (!dateValue || !timeValue) {
            alert("Please select both a publish date and time.");
            return;
        }

        const scheduled = new Date(`${dateValue}T${timeValue}`);
        const now = new Date();

        if (scheduled <= now) {
            alert("Scheduled time must be in the future.");
            return;
        }

        form.append("publish-date", dateValue);
        form.append("publish-time", timeValue);
    }

    try {
        const response = await fetch("/api/posts/publish", {
            method: "POST",
            body: form
        });

        const result = await response.json();
        alert(result.Message || "Post submitted successfully!");
    } catch (error) {
        console.error("Submission failed:", error);
        alert("Something went wrong during publishing.");
    }
});



