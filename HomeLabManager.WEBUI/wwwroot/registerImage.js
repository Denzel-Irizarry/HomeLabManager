window.triggerFileInputClick = (inputId) => {
    // Get the input element by its ID and trigger a click event to open the file dialog
    const input = document.getElementById(inputId);
    if (input) input.click();
};

window.registerImageDropZone = (dropZoneId, inputId) => {
    // Get the drop zone and input elements by their IDs
    const dropZone = document.getElementById(dropZoneId);
    const input = document.getElementById(inputId);

    // If either the drop zone or input element is not found, exit the function
    if (!dropZone || !input) return;

    // Add event listeners for drag and drop events on the drop zone
    const preventDefaults = (e) => {
        e.preventDefault();
        e.stopPropagation();
    };

    // Prevent default behavior for drag and drop events to allow dropping files
    ['dragenter', 'dragover', 'dragleave', 'drop'].forEach(eventName => {
        dropZone.addEventListener(eventName, preventDefaults, false);
    });


    // Add a drop event listener to handle file drops on the drop zone
    dropZone.addEventListener("dragover", () => dropZone.classList.add("bg-light"), false);
    dropZone.addEventListener("dragleave", () => dropZone.classList.remove("bg-light"), false);

    // Handle the drop event to get the dropped files and set them to the input element
    dropZone.addEventListener("drop", (e) => {
        dropZone.classList.remove("bg-light");

        // Get the files from the drop event
        const files = e.dataTransfer?.files;

        // If no files were dropped, exit the function
        if (!files || files.length === 0) return;

        // Set the dropped files to the input element
        input.files = files;

        // Trigger change event to notify Blazor of the file change)
        input.dispatchEvent(new Event("change", { bubbles: true })); 
    }, false);

};

// Function to clear the file input value, allowing users to select the same file again if needed
window.clearFileInput = (inputId) => {
    // Get the input element by its ID
    const input = document.getElementById(inputId);

    // If the input element is not found, exit the function
    if (!input) return;

    input.value = ""; // Clear the file input value
}
