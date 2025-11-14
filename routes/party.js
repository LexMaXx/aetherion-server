// routes/party.js
// REST API для управления группами (party system)

const express = require('express');
const router = express.Router();
const mongoose = require('mongoose');
const Party = require('../models/Party');
const auth = require('../middleware/auth');
const { v4: uuidv4 } = require('uuid');

/**
 * POST /api/party/create
 * Создать новую группу
 */
router.post('/create', auth, async (req, res) => {
    try {
        const userId = new mongoose.Types.ObjectId(req.user.id);
        const { roomId, username, characterClass, level, health, mana } = req.body;

        // Проверяем что игрок не в другой группе
        const existingParty = await Party.findOne({
            'members.userId': userId
        });

        if (existingParty) {
            return res.status(400).json({
                success: false,
                message: 'You are already in a party'
            });
        }

        // Создаём новую группу
        const partyId = uuidv4();
        const party = new Party({
            partyId,
            leaderId: userId,
            roomId,
            members: [{
                userId,
                username,
                characterClass,
                level: level || 1,
                health: health || { current: 100, max: 100 },
                mana: mana || { current: 100, max: 100 }
            }]
        });

        await party.save();

        console.log(`[Party] Created: ${partyId} by ${username}`);

        res.json({
            success: true,
            message: 'Party created',
            party: {
                partyId: party.partyId,
                leaderId: party.leaderId,
                members: party.members,
                maxMembers: party.maxMembers
            }
        });

    } catch (error) {
        console.error('[Party] Create error:', error);
        res.status(500).json({
            success: false,
            message: 'Failed to create party'
        });
    }
});

/**
 * GET /api/party/info/:partyId
 * Получить информацию о группе
 */
router.get('/info/:partyId', auth, async (req, res) => {
    try {
        const { partyId } = req.params;

        const party = await Party.findOne({ partyId });

        if (!party) {
            return res.status(404).json({
                success: false,
                message: 'Party not found'
            });
        }

        res.json({
            success: true,
            party: {
                partyId: party.partyId,
                leaderId: party.leaderId,
                members: party.members,
                maxMembers: party.maxMembers,
                createdAt: party.createdAt
            }
        });

    } catch (error) {
        console.error('[Party] Get info error:', error);
        res.status(500).json({
            success: false,
            message: 'Failed to get party info'
        });
    }
});

/**
 * GET /api/party/my
 * Получить группу текущего игрока
 */
router.get('/my', auth, async (req, res) => {
    try {
        const userId = new mongoose.Types.ObjectId(req.user.id);

        const party = await Party.findOne({
            'members.userId': userId
        });

        if (!party) {
            return res.json({
                success: true,
                party: null,
                message: 'Not in a party'
            });
        }

        res.json({
            success: true,
            party: {
                partyId: party.partyId,
                leaderId: party.leaderId,
                members: party.members,
                maxMembers: party.maxMembers
            }
        });

    } catch (error) {
        console.error('[Party] Get my party error:', error);
        res.status(500).json({
            success: false,
            message: 'Failed to get party'
        });
    }
});

/**
 * POST /api/party/leave
 * Покинуть группу
 */
router.post('/leave', auth, async (req, res) => {
    try {
        const userId = new mongoose.Types.ObjectId(req.user.id);

        const party = await Party.findOne({
            'members.userId': userId
        });

        if (!party) {
            return res.status(404).json({
                success: false,
                message: 'You are not in a party'
            });
        }

        await party.removeMember(userId);

        // Если группа пустая - удаляем её
        if (party.members.length === 0) {
            await Party.deleteOne({ partyId: party.partyId });
            console.log(`[Party] Deleted empty party ${party.partyId}`);
        }

        console.log(`[Party] ${req.user.username} left party ${party.partyId}`);

        res.json({
            success: true,
            message: 'Left party successfully'
        });

    } catch (error) {
        console.error('[Party] Leave error:', error);
        res.status(500).json({
            success: false,
            message: error.message || 'Failed to leave party'
        });
    }
});

module.exports = router;
