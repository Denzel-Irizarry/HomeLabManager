(() => {
    const pageId = "register-image-page";

    function setStatus(statusElement, wrapperElement, devicesLinkElement, message, isError) {
        statusElement.textContent = message;
        statusElement.className = isError ? "text-danger mb-0" : "text-success mb-0";
        wrapperElement.classList.remove("d-none");
        devicesLinkElement.classList.remove("d-none");
    }

    function clearSelection(inputElement, selectionElement, nameElement, submitElement) {
        inputElement.value = "";
        nameElement.textContent = "";
        selectionElement.classList.add("d-none");
        selectionElement.classList.remove("d-flex");
        submitElement.disabled = true;
    }

    function initRegisterImagePage() {
        const page = document.getElementById(pageId);
        if (!page || page.dataset.initialized === "true") {
            return;
        }

        page.dataset.initialized = "true";

        const apiBaseUrl = page.dataset.apiBaseUrl;
        const input = document.getElementById("register-image-input");
        const selection = document.getElementById("register-image-selection");
        const name = document.getElementById("register-image-name");
        const submit = document.getElementById("register-image-submit");
        const clear = document.getElementById("register-image-clear");
        const statusWrapper = document.getElementById("register-image-status-wrapper");
        const status = document.getElementById("register-image-status");
        const devicesLink = document.getElementById("register-image-devices-link");

        if (!apiBaseUrl || !input || !selection || !name || !submit || !clear || !statusWrapper || !status || !devicesLink) {
            return;
        }

        input.addEventListener("change", () => {
            const file = input.files && input.files.length > 0 ? input.files[0] : null;

            statusWrapper.classList.add("d-none");
            devicesLink.classList.add("d-none");

            if (!file) {
                clearSelection(input, selection, name, submit);
                return;
            }

            name.textContent = file.name;
            selection.classList.remove("d-none");
            selection.classList.add("d-flex");
            submit.disabled = false;
        });

        clear.addEventListener("click", () => {
            clearSelection(input, selection, name, submit);
        });

        submit.addEventListener("click", async () => {
            const file = input.files && input.files.length > 0 ? input.files[0] : null;
            if (!file) {
                setStatus(status, statusWrapper, devicesLink, "No file selected. Please select an image to upload.", true);
                return;
            }

            submit.disabled = true;

            try {
                const formData = new FormData();
                formData.append("file", file, file.name);

                const response = await fetch(`${apiBaseUrl}/api/devices/register`, {
                    method: "POST",
                    body: formData
                });

                const responseText = await response.text();

                if (response.ok) {
                    setStatus(status, statusWrapper, devicesLink, "Image uploaded and device registered successfully!", false);
                    clearSelection(input, selection, name, submit);
                    return;
                }

                if (response.status === 409) {
                    setStatus(status, statusWrapper, devicesLink, "A device with the same identifier already exists. Please check the image and try again.", true);
                    return;
                }

                if (response.status === 400) {
                    setStatus(
                        status,
                        statusWrapper,
                        devicesLink,
                        responseText && responseText.trim().length > 0
                            ? responseText
                            : "The uploaded image is invalid or unreadable. Please check the image and try again.",
                        true
                    );
                    return;
                }

                setStatus(status, statusWrapper, devicesLink, `Failed to register: ${response.status} - ${responseText}`, true);
            } catch (error) {
                const message = error instanceof Error ? error.message : "Unexpected upload error.";
                setStatus(status, statusWrapper, devicesLink, `An error occurred while uploading the image: ${message}`, true);
            } finally {
                const hasFile = input.files && input.files.length > 0;
                submit.disabled = !hasFile;
            }
        });
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", initRegisterImagePage);
    } else {
        initRegisterImagePage();
    }

    document.addEventListener("enhancedload", initRegisterImagePage);
    window.addEventListener("pageshow", initRegisterImagePage);
})();
