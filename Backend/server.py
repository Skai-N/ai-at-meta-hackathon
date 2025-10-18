# LlaMaps Backend Server
# FastAPI server for handling navigation requests

import asyncio
import json
import logging
import os
import uvicorn
from typing import List, Optional

import polyline
import requests
from fastapi import FastAPI, HTTPException
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel

app = FastAPI(title="LlaMaps Backend", version="1.0.0")

# Enable CORS for Quest and phone communication
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# Configuration
GOOGLE_MAPS_API_KEY = os.getenv("GOOGLE_MAPS_API_KEY", "")

# Data models
class LocationData(BaseModel):
    latitude: float
    longitude: float

class RouteRequest(BaseModel):
    destination: str
    travelMode: str = "WALK"


# In-memory storage for current location (in production, use Redis/database)
current_location = LocationData(latitude=37.4843566, longitude=-122.1475263)

@app.get("/")
async def root():
    return {"message": "LlaMaps Backend Server", "status": "running"}

@app.post("/api/location")
async def update_location(location: LocationData):
    """Update current location from phone GPS"""
    global current_location
    current_location = location
    print(f"Location updated: {location.latitude}, {location.longitude}")
    return {"status": True}

@app.get("/api/location")
async def get_location():
    """Get current location for Quest"""
    if current_location is None:
        raise HTTPException(status_code=404, detail="No location data available")
    
    return {
       "position": {
            "latitude": current_location,
            "longitude": current_location.longitude,
        },
        "success": True
    }

@app.post("/api/route")
async def find_route(request: RouteRequest):
    """Get route from Google Maps API"""
    try:
        # First, resolve destination with Google Places if needed
        destination_coords = await resolve_destination(request.destination, current_location)
        
        # Get route from Google Directions API
        coords = await get_google_route(current_location, destination_coords, request.travelMode)
        
        return {"success": True, "coords": coords}
        
    except Exception as e:
        print(f"Route calculation error: {str(e)}")
        return {
            "success": False,
            "error": f"Route calculation failed: {str(e)}"
        }

async def resolve_destination(destination: str, origin: LocationData):
    """Resolve destination string to coordinates using Google Places API"""
    
    # If destination already looks like coordinates, parse it
    if "," in destination and destination.replace(",", "").replace(".", "").replace("-", "").replace(" ", "").isdigit():
        parts = destination.split(",")
        if len(parts) == 2:
            try:
                lat = float(parts[0].strip())
                lng = float(parts[1].strip())
                return LocationData(latitude=lat, longitude=lng)
            except ValueError:
                pass
    
    # Use Google Places API to find the destination
    places_url = "https://maps.googleapis.com/maps/api/place/findplacefromtext/json"
    params = {
        "input": destination,
        "inputtype": "textquery",
        "fields": "geometry",
        "locationbias": f"circle:10000@{origin.latitude},{origin.longitude}",
        "key": GOOGLE_MAPS_API_KEY
    }
    
    response = requests.get(places_url, params=params)
    data = response.json()
    
    if data["status"] == "OK" and data["candidates"]:
        location = data["candidates"][0]["geometry"]["location"]
        return LocationData(latitude=location["lat"], longitude=location["lng"])
    else:
        raise Exception(f"Could not find destination: {destination}")

async def get_google_route(origin: LocationData, destination: LocationData, travel_mode: str):
    """Get route from Google Directions API"""
    
    directions_url = "https://maps.googleapis.com/maps/api/directions/json"
    params = {
        "origin": f"{origin.latitude},{origin.longitude}",
        "destination": f"{destination.latitude},{destination.longitude}",
        "mode": travel_mode.lower(),
        "key": GOOGLE_MAPS_API_KEY
    }
    
    response = requests.get(directions_url, params=params)
    data = response.json()
    
    if data["status"] != "OK":
        raise Exception(f"Google Directions API error: {data['status']}")
    
    route = data["routes"][0]
    
    # Decode polyline using the polyline library
    encoded_polyline = route["overview_polyline"]["points"]
    coords = polyline.decode(encoded_polyline)
    
    # Convert to lat/lng objects for consistency
    formatted_coords = [{"lat": lat, "lng": lng} for lat, lng in coords]

    return formatted_coords


if __name__ == "__main__":
    uvicorn.run(app, host="localhost", port=8000)
