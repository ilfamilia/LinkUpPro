document.addEventListener("DOMContentLoaded", function () {
    const mediaType = document.getElementById("MediaType");
    const imageSection = document.getElementById("imageSection");
    const videoSection = document.getElementById("videoSection");
    const imageFile = document.getElementById("ImageFile");
    const youtubeUrl = document.getElementById("YouTubeUrl");

    if (!mediaType || !imageSection || !videoSection ||
        !imageFile || !youtubeUrl) {
        return;
    }

    const imageValue = mediaType.dataset.imageValue;
    const videoValue = mediaType.dataset.videoValue;

    function updateMediaFields(clearValues = false) {
        const isImage = mediaType.value === imageValue;
        const isVideo = mediaType.value === videoValue;

        imageSection.classList.toggle("d-none", !isImage);
        videoSection.classList.toggle("d-none", !isVideo);

        if (clearValues) {
            if (!isImage) imageFile.value = "";
            if (!isVideo) youtubeUrl.value = "";
        }
    }

    mediaType.addEventListener("change", function () {
        updateMediaFields(true);
    });

    updateMediaFields();
});