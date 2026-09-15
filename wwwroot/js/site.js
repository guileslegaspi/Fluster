
let formToSubmit = null;

document.addEventListener("DOMContentLoaded", function () {

    const confirmationModalElement =
        document.getElementById("confirmationModal");

    if (confirmationModalElement) {

        const confirmationModal =
            new bootstrap.Modal(confirmationModalElement);

        const modalTitle =
            document.getElementById("confirmationModalLabel");

        const modalMessage =
            document.getElementById("confirmationModalMessage");

        const modalButton =
            document.getElementById("confirmationModalButton");


        document.querySelectorAll(".js-confirm-form").forEach(function (form) {

            form.addEventListener("submit", function (event) {

                event.preventDefault();

                formToSubmit = form;

                modalTitle.textContent =
                    form.dataset.confirmTitle || "Confirm Action";

                modalMessage.textContent =
                    form.dataset.confirmMessage || "Are you sure?";

                modalButton.textContent =
                    form.dataset.confirmButton || "Yes, Continue";

                confirmationModal.show();
            });

        });


        modalButton.addEventListener("click", function () {

            if (formToSubmit !== null) {

                formToSubmit.submit();

                formToSubmit = null;

                confirmationModal.hide();
            }

        });

    }

});

