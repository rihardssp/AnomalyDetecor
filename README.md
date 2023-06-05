# Anomaly detector api

## Description

The purpose is to provide a way for devices to send any form of metrics to the api and receive real-time responses for if there's been an anomaly in the last x entries with the help of a trained model.
The model is trained on the data of the last 10 years (could be adjustable in the future).

## Table of Contents

- [Installation](#installation)
- [Usage](#usage)

## Installation

Use https://anomalydetecor.azurewebsites.net/index.html

## Usage

- Decide on a unique device name, so you don't end up clashing with anybody else (no checks were added yet).
- First add some data (at least 30 entries). Preferably hourly entries and preferably multi-variable entries (more than 1 reading).
- Then select to train the device.
- Once trained every next entry will be evaluated against this model (as part of the last n entries, which currently is set to 30).
- Batch additions are not evaluated.


To use batch addition create a csv with the first column being the datetime format in "2021-01-04T00:00:00Z" form, the rest of columns are readings of sensors in float format.
